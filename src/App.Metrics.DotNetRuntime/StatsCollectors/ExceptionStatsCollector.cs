using System.Diagnostics.Tracing;
using App.Metrics.DotNetRuntime.EventSources;

namespace App.Metrics.DotNetRuntime.StatsCollectors
{
    internal sealed class ExceptionStatsCollector : IEventSourceStatsCollector
    {
        private IMetrics _metrics;
        private const int ExceptionThrown_V1 = 80;

        public ExceptionStatsCollector(IMetrics metrics)
        {
            _metrics = metrics;
        }

        public string EventSourceName => DotNetRuntimeEventSource.Name;
        public EventKeywords Keywords => (EventKeywords) DotNetRuntimeEventSource.Keywords.Exception;
        public EventLevel Level => EventLevel.Verbose;

        public void ProcessEvent(EventWrittenEventArgs e)
        {
            if (e.EventId == ExceptionThrown_V1)
            {
                _metrics.Measure.Meter.Mark(DotNetRuntimeMetricsRegistry.Meters.ExceptionsThrown);
            }
        }
    }
}
