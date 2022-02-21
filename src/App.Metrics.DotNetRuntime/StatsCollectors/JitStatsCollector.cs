using System.Diagnostics.Tracing;
using App.Metrics.DotNetRuntime.EventSources;
using App.Metrics.DotNetRuntime.StatsCollectors.Util;
using App.Metrics.Gauge;
using App.Metrics.Timer;

namespace App.Metrics.DotNetRuntime.StatsCollectors
{
    /// <summary>
    /// Measures the activity of the JIT (Just In Time) compiler in a process.
    /// Tracks how often it runs and how long it takes to compile methods
    /// </summary>
    internal sealed class JitStatsCollector : IEventSourceStatsCollector
    {
        private readonly IMetrics _metrics;
        private const int EventIdMethodJittingStarted = 145, EventIdMethodLoadVerbose = 143;
        private const string DynamicLabel = "dynamic";
        private const string LabelValueTrue = "true";
        private const string LabelValueFalse = "false";
        private const double NanosPerMilliSecond = 1000000.0;

        private readonly EventPairTimer<ulong> _eventPairTimer;

        private readonly ProcessTotalCpuTimer _jitCpuProcessTotalCpuTimer = new ProcessTotalCpuTimer();

        public JitStatsCollector(IMetrics metrics)
        {
            _metrics = metrics;
            _eventPairTimer = new EventPairTimer<ulong>(
                EventIdMethodJittingStarted,
                EventIdMethodLoadVerbose,
                x => (ulong)x.Payload[0]
            );
        }

        public EventKeywords Keywords => (EventKeywords) DotNetRuntimeEventSource.Keywords.Jit;
        public EventLevel Level => EventLevel.Verbose;
        public string EventSourceName => DotNetRuntimeEventSource.Name;

        public void ProcessEvent(EventWrittenEventArgs e)
        {
            if (_eventPairTimer.TryGetDuration(e, out var duration) == DurationResult.FinalWithDuration)
            {
                // dynamic methods are of special interest to us- only a certain number of JIT'd dynamic methods
                // will be cached. Frequent use of dynamic can cause methods to be evicted from the cache and re-JIT'd
                var methodFlags = (uint)e.Payload[5];
                var dynamicLabelValue = (methodFlags & 0x1) == 0x1 ? LabelValueTrue : LabelValueFalse;

                _metrics.Provider.Timer.Instance(DotNetRuntimeMetricsRegistry.Timers.MethodsJittedMilliSecondsTotal,
                    new MetricTags(DynamicLabel, dynamicLabelValue)).Record(duration.Ticks * 100, TimeUnit.Nanoseconds);
                _metrics.Provider.Timer.Instance(DotNetRuntimeMetricsRegistry.Timers.MethodsJittedMilliSecondsTotal)
                    .Record(duration.Ticks * 100, TimeUnit.Nanoseconds);

                var methodsJittedMsTotalCounter = _metrics.Provider.Timer.Instance(DotNetRuntimeMetricsRegistry.Timers.MethodsJittedMilliSecondsTotal);
                _jitCpuProcessTotalCpuTimer.Calculate();
                _metrics.Measure.Gauge.SetValue(DotNetRuntimeMetricsRegistry.Gauges.CpuRatio,
                    () => new RatioGauge(
                        () => methodsJittedMsTotalCounter.GetValueOrDefault().Histogram.LastValue,
                        () => _jitCpuProcessTotalCpuTimer.ProcessTimeUsed.Ticks * 100.0));
            }
        }
    }
}
