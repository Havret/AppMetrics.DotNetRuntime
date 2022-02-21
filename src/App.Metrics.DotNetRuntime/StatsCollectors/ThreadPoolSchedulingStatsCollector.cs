using System;
using System.Diagnostics.Tracing;
using App.Metrics.DotNetRuntime.EventSources;
using App.Metrics.DotNetRuntime.StatsCollectors.Util;

namespace App.Metrics.DotNetRuntime.StatsCollectors
{
    /// <summary>
    /// Measures the volume of work scheduled on the thread pool and the delay between scheduling the work and it beginning execution.
    /// </summary>
    internal sealed class ThreadPoolSchedulingStatsCollector : IEventSourceStatsCollector
    {
        private const int EventIdThreadPoolEnqueueWork = 30, EventIdThreadPoolDequeueWork = 31;
        private readonly IMetrics _metrics;

        private readonly EventPairTimer<long> _eventPairTimer;

        public ThreadPoolSchedulingStatsCollector(IMetrics metrics)
        {
            _metrics = metrics;
            _eventPairTimer  = new EventPairTimer<long>(
                EventIdThreadPoolEnqueueWork, 
                EventIdThreadPoolDequeueWork, 
                x => (long)x.Payload[0],
                new Cache<long, int>(TimeSpan.FromSeconds(30), initialCapacity: 512)
            );
        }

        public EventKeywords Keywords => (EventKeywords) (FrameworkEventSource.Keywords.ThreadPool);
        public EventLevel Level => EventLevel.Verbose;
        public string EventSourceName => DotNetRuntimeEventSource.Name;
        
        public void ProcessEvent(EventWrittenEventArgs e)
        {
            if(_eventPairTimer.TryGetDuration(e, out var duration) != DurationResult.FinalWithDuration)
            {
                return;
            }
            
            _metrics.Provider.Timer.Instance(DotNetRuntimeMetricsRegistry.Timers.ScheduleDelay).Record(duration.Ticks * 100, TimeUnit.Nanoseconds);
        }
    }
}