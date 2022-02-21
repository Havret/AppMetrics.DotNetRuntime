using System.Collections.Generic;
using System.Diagnostics.Tracing;
using App.Metrics.DotNetRuntime.EventSources;
using App.Metrics.DotNetRuntime.StatsCollectors.Util;

namespace App.Metrics.DotNetRuntime.StatsCollectors
{
    /// <summary>
    /// Measures the size of the worker + IO thread pools, worker pool throughput and reasons for worker pool
    /// adjustments. 
    /// </summary>
    public class ThreadPoolStatsCollector : IEventSourceStatsCollector
    {
        private readonly IMetrics _metrics;

        private const int
            EventIdThreadPoolSample = 54,
            EventIdThreadPoolAdjustment = 55,
            EventIdIoThreadCreate = 44,
            EventIdIoThreadRetire = 46,
            EventIdIoThreadUnretire = 47,
            EventIdIoThreadTerminate = 45;

        private Dictionary<DotNetRuntimeEventSource.ThreadAdjustmentReason, string> _adjustmentReasonToLabel = LabelGenerator.MapEnumToLabelValues<DotNetRuntimeEventSource.ThreadAdjustmentReason>();

        public ThreadPoolStatsCollector(IMetrics metrics)
        {
            _metrics = metrics;
        }

        public string EventSourceName => DotNetRuntimeEventSource.Name;
        public EventKeywords Keywords => (EventKeywords) DotNetRuntimeEventSource.Keywords.Threading;
        public EventLevel Level => EventLevel.Informational;

        public void ProcessEvent(EventWrittenEventArgs e)
        {
            switch (e.EventId)
            {
                case EventIdThreadPoolSample:
                    // Throughput.Inc((double) e.Payload[0]);
                    return;

                case EventIdThreadPoolAdjustment:
                    _metrics.Measure.Gauge.SetValue(DotNetRuntimeMetricsRegistry.Gauges.NumThreads, (uint) e.Payload[1]);
                    _metrics.Measure.Meter.Mark(DotNetRuntimeMetricsRegistry.Meters.AdjustmentsTotal, new MetricTags("reason", _adjustmentReasonToLabel[(DotNetRuntimeEventSource.ThreadAdjustmentReason) e.Payload[2]]));
                    return;

                case EventIdIoThreadCreate:
                case EventIdIoThreadRetire:
                case EventIdIoThreadUnretire:
                case EventIdIoThreadTerminate:
                    // doesn't look like these events are correctly emitted. disabling for now.
                    //    _metrics.Measure.Gauge.SetValue(DotNetRuntimeMetricsRegistry.Gauges.NumIoThreads, (uint) e.Payload[1]);
                    return;
            }
        }
    }
}
