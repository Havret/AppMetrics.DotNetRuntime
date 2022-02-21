using System;
using System.Diagnostics.Tracing;
using App.Metrics.DotNetRuntime.EventSources;
using App.Metrics.DotNetRuntime.StatsCollectors.Util;

namespace App.Metrics.DotNetRuntime.StatsCollectors
{
    /// <summary>
    /// Measures the level of contention in a .NET process, capturing the number 
    /// of locks contended and the total amount of time spent contending a lock.
    /// </summary>
    /// <remarks>
    /// Due to the way ETW events are triggered, only monitors contended will fire an event- spin locks, etc.
    /// do not trigger contention events and so cannot be tracked.
    /// </remarks>
    internal sealed class ContentionStatsCollector : IEventSourceStatsCollector
    {
        private readonly IMetrics _metrics;
        private const int EventIdContentionStart = 81, EventIdContentionStop = 91;
        private readonly EventPairTimer<long> _eventPairTimer;

        public ContentionStatsCollector(IMetrics metrics)
        {
            _metrics = metrics;
            _eventPairTimer = new EventPairTimer<long>(
                EventIdContentionStart,
                EventIdContentionStop,
                x => x.OSThreadId
            );
        }

        public EventKeywords Keywords => (EventKeywords)DotNetRuntimeEventSource.Keywords.Contention;
        public EventLevel Level => EventLevel.Informational;
        public string EventSourceName => DotNetRuntimeEventSource.Name;
        
        public void ProcessEvent(EventWrittenEventArgs e)
        {
            if(_eventPairTimer.TryGetDuration(e, out var duration) != DurationResult.FinalWithDuration)
            {
                return;
            }
            
            _metrics.Provider.Timer.Instance(DotNetRuntimeMetricsRegistry.Timers.ContentionMilliSecondsTotal).Record(duration.Ticks * 100, TimeUnit.Nanoseconds);
        }
    }
}