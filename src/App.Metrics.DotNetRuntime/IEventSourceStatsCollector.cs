using System;
using System.Diagnostics.Tracing;

namespace App.Metrics.DotNetRuntime
{
    /// <summary>
    /// Defines an interface register for and receive .NET runtime events. Events can then be aggregated
    /// and measured as AppMetrics metrics.
    /// </summary>
    public interface IEventSourceStatsCollector
    {
        /// <summary>
        /// The name of the event source to receive events from.
        /// </summary>
        public string EventSourceName { get; }
        
        /// <summary>
        /// The keywords to enable in the event source.
        /// </summary>
        /// <remarks>
        /// Keywords act as a "if-any-match" filter- specify multiple keywords to obtain multiple categories of events
        /// from the event source.
        /// </remarks>
        EventKeywords Keywords { get; }
        
        /// <summary>
        /// The level of events to receive from the event source.
        /// </summary>
        EventLevel Level { get; }
        
        /// <summary>
        /// Process a received event.
        /// </summary>
        /// <remarks>
        /// Implementors should listen to events and perform some kind of aggregation, emitting this to AppMetrics.
        /// </remarks>
        void ProcessEvent(EventWrittenEventArgs e);
    }
}