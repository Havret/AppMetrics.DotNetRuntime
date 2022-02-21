using System;
using System.Diagnostics.Tracing;
using App.Metrics.Counter;
using App.Metrics.DotNetRuntime.StatsCollectors.Util;
using App.Metrics.Timer;

namespace App.Metrics.DotNetRuntime
{
    internal sealed class DotNetEventListener : EventListener
    {
        private static TimerOptions _cpuConsumed;
        private static CounterOptions _eventTypeCounts;

        private readonly IEventSourceStatsCollector _collector;
        private readonly Action<Exception> _errorHandler;
        private readonly bool _enableDebugging;
        private readonly IMetrics _metrics;

        public DotNetEventListener(IEventSourceStatsCollector collector, Action<Exception> errorHandler, bool enableDebugging, IMetrics metrics) : base()
        {
            _collector = collector;
            _errorHandler = errorHandler;
            _enableDebugging = enableDebugging;
            _metrics = metrics;

            if (_enableDebugging)
            {
                _cpuConsumed = new TimerOptions()
                {
                    Context = DotNetRuntimeMetricsRegistry.ContextName,
                    MeasurementUnit = Unit.None,
                    DurationUnit = TimeUnit.Nanoseconds,
                    Name = "dotnet_debug_cpu_nanoseconds_total",
                    Tags = new MetricTags("collector", collector.GetType().Name.ToSnakeCase())
                };
            }

            _eventTypeCounts = new CounterOptions()
            {
                Context = DotNetRuntimeMetricsRegistry.ContextName,
                MeasurementUnit = Unit.Items,
                ReportItemPercentages = false
            };
            EventSourceCreated += OnEventSourceCreated;
        }

        internal bool StartedReceivingEvents { get; private set; }

        private void OnEventSourceCreated(object sender, EventSourceCreatedEventArgs e)
        {
            var es = e.EventSource;
            if (es.Name == _collector.EventSourceName)
            {
                EnableEvents(es, _collector.Level, _collector.Keywords);
                StartedReceivingEvents = true;
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            try
            {
                if (_enableDebugging)
                {
                    _metrics.Provider.Timer.Instance(_cpuConsumed,
                            new MetricTags(new[] {"EventSource", "EventName"},
                                new[] {eventData.EventSource.Name, eventData.EventName}))
                        .Time(() => _collector.ProcessEvent(eventData));
                    return;
                }

                _collector.ProcessEvent(eventData);
            }
            catch (Exception e)
            {
                _errorHandler(e);
            }
        }

        public override void Dispose()
        {
            EventSourceCreated -= OnEventSourceCreated;
            base.Dispose();
        }
    }
}