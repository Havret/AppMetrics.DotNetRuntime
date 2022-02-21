using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using App.Metrics.DotNetRuntime.EventSources;
using App.Metrics.DotNetRuntime.StatsCollectors.Util;
using App.Metrics.Gauge;
using App.Metrics.Timer;

namespace App.Metrics.DotNetRuntime.StatsCollectors
{
    /// <summary>
    /// Measures how the frequency and duration of garbage collections and volume of allocations. Includes information
    ///  such as the generation the collection is running for, what triggered the collection and the type of the collection.
    /// </summary>
    internal sealed class GcStatsCollector : IEventSourceStatsCollector
    {
        private const string
            LabelHeap = "gc_heap",
            LabelGeneration = "gc_generation",
            LabelReason = "gc_reason",
            LabelType = "gc_type";

        private const int
            EventIdGcStart = 1,
            EventIdGcStop = 2,
            EventIdSuspendEEStart = 9,
            EventIdRestartEEStop = 3,
            EventIdHeapStats = 4,
            EventIdAllocTick = 10;
        private const double NanosPerMilliSecond = 1000000.0;
        private readonly EventPairTimer<uint, GcData> _gcEventTimer = new EventPairTimer<uint, GcData>(
            EventIdGcStart,
            EventIdGcStop,
            x => (uint) x.Payload[0],
            x => new GcData((uint) x.Payload[1], (DotNetRuntimeEventSource.GCType) x.Payload[3]));

        private readonly EventPairTimer<int, DateTime> _gcPauseEventTimer = new EventPairTimer<int, DateTime>(
            EventIdSuspendEEStart,
            EventIdRestartEEStop,
            // Suspensions/ Resumptions are always done sequentially so there is no common value to match events on. Return a constant value as the event id.
            x => 1,
            (e) => e.TimeStamp);

        private readonly Dictionary<DotNetRuntimeEventSource.GCReason, string> _gcReasonToLabels = LabelGenerator.MapEnumToLabelValues<DotNetRuntimeEventSource.GCReason>();
        private readonly ProcessTotalCpuTimer _gcCpuProcessTotalCpuTimer = new ProcessTotalCpuTimer();
        private readonly IMetrics _metrics;

        public GcStatsCollector(IMetrics metrics)
        {
            _metrics = metrics;
        }

        public string EventSourceName => DotNetRuntimeEventSource.Name;
        public EventKeywords Keywords => (EventKeywords) DotNetRuntimeEventSource.Keywords.GC;
        public EventLevel Level => EventLevel.Verbose;

        public void ProcessEvent(EventWrittenEventArgs e)
        {
            if (e.EventId == EventIdAllocTick)
            {
                const uint lohHeapFlag = 0x1;
                var heapLabelValue = ((uint) e.Payload[1] & lohHeapFlag) == lohHeapFlag ? "loh" : "soh";
                _metrics.Measure.Meter.Mark(DotNetRuntimeMetricsRegistry.Meters.AllocatedBytes, new MetricTags("heap", heapLabelValue), Convert.ToInt64((UInt64)e.Payload[3]));
                return;
            }

            if (e.EventId == EventIdHeapStats)
            {
                _metrics.Measure.Gauge.SetValue(DotNetRuntimeMetricsRegistry.Gauges.GcHeapSizeBytes, new MetricTags("generation", "0"), (UInt64)e.Payload[0]);
                _metrics.Measure.Gauge.SetValue(DotNetRuntimeMetricsRegistry.Gauges.GcHeapSizeBytes, new MetricTags("generation", "1"), (UInt64)e.Payload[2]);
                _metrics.Measure.Gauge.SetValue(DotNetRuntimeMetricsRegistry.Gauges.GcHeapSizeBytes, new MetricTags("generation", "2"), (UInt64)e.Payload[4]);
                _metrics.Measure.Gauge.SetValue(DotNetRuntimeMetricsRegistry.Gauges.GcHeapSizeBytes, new MetricTags("generation", "loh"), (UInt64)e.Payload[6]);
                _metrics.Measure.Gauge.SetValue(DotNetRuntimeMetricsRegistry.Gauges.GcFinalizationQueueLength, (UInt64)e.Payload[9]);
                _metrics.Measure.Gauge.SetValue(DotNetRuntimeMetricsRegistry.Gauges.GcNumPinnedObjects, (UInt32)e.Payload[10]);
                return;
            }

            // flags representing the "Garbage Collection" + "Preparation for garbage collection" pause reasons
            const uint suspendGcReasons = 0x1 | 0x6;

            if (e.EventId == EventIdSuspendEEStart && ((uint) e.Payload[0] & suspendGcReasons) == 0)
            {
                // Execution engine is pausing for a reason other than GC, discard event.
                return;
            }

            if (_gcPauseEventTimer.TryGetDuration(e, out var pauseDuration, out var startEventData) == DurationResult.FinalWithDuration)
            {
                var timeSinceEventStarted = DateTime.UtcNow - startEventData;
                var gcPauseMilliSecondsTimer =
                    _metrics.Provider.Timer.Instance(DotNetRuntimeMetricsRegistry.Timers.GcPauseMilliSeconds);
                 gcPauseMilliSecondsTimer.Record(pauseDuration.Ticks * 100, TimeUnit.Nanoseconds);
                 _metrics.Measure.Gauge.SetValue(DotNetRuntimeMetricsRegistry.Gauges.GcPauseRatio,
                     () => new RatioGauge(
                         () => gcPauseMilliSecondsTimer.GetValueOrDefault().Histogram.LastValue/NanosPerMilliSecond,
                         () => timeSinceEventStarted.TotalMilliseconds));
                 return;
            }

            if (e.EventId == EventIdGcStart)
            {
                _metrics.Measure.Meter.Mark(DotNetRuntimeMetricsRegistry.Meters.GcCollectionReasons, new MetricTags("reason", _gcReasonToLabels[(DotNetRuntimeEventSource.GCReason) e.Payload[2]]));
            }

            if (_gcEventTimer.TryGetDuration(e, out var gcDuration, out var gcData) == DurationResult.FinalWithDuration)
            {
                var gcCollectionMilliSecondsTimer =
                    _metrics.Provider.Timer.Instance(DotNetRuntimeMetricsRegistry.Timers.GcCollectionMilliSeconds);
                gcCollectionMilliSecondsTimer.Record(gcDuration.Ticks * 100, TimeUnit.Nanoseconds);

                _metrics.Measure.Gauge.SetValue(DotNetRuntimeMetricsRegistry.Gauges.GcCpuRatio, () =>
                {
                    _gcCpuProcessTotalCpuTimer.Calculate();
                    return new RatioGauge(
                        () => gcCollectionMilliSecondsTimer.GetValueOrDefault().Histogram.LastValue,
                        () => _gcCpuProcessTotalCpuTimer.ProcessTimeUsed.Ticks*100);
                });
            }
        }

        private struct GcData
        {
            private static readonly Dictionary<DotNetRuntimeEventSource.GCType, string> GcTypeToLabels = LabelGenerator.MapEnumToLabelValues<DotNetRuntimeEventSource.GCType>();

            public GcData(uint generation, DotNetRuntimeEventSource.GCType type)
            {
                Generation = generation;
                Type = type;
            }

            public uint Generation { get; }
            public DotNetRuntimeEventSource.GCType Type { get; }

            public string GetTypeToString()
            {
                return GcTypeToLabels[Type];
            }

            public string GetGenerationToString()
            {
                if (Generation > 2)
                {
                    return "loh";
                }

                return Generation.ToString();
            }
        }
    }
}
