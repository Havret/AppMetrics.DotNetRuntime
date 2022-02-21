using App.Metrics.Gauge;
using App.Metrics.Meter;
using App.Metrics.Timer;

namespace App.Metrics.DotNetRuntime
{
    public static class DotNetRuntimeMetricsRegistry
    {
        public static string ContextName = "dotnet";
        
        public static class Timers
        {
            public static readonly TimerOptions MethodsJittedMilliSecondsTotal = new TimerOptions()
            {
                Context = ContextName,
                Name = "jit_method_milliseconds",
                DurationUnit = TimeUnit.Milliseconds,
                RateUnit = TimeUnit.Minutes
            };
            
            public static readonly TimerOptions ContentionMilliSecondsTotal = new TimerOptions()
            {
                Context = ContextName,
                Name = "contention_milliseconds",
                DurationUnit = TimeUnit.Milliseconds,
                RateUnit = TimeUnit.Minutes
            };
            
            public static readonly TimerOptions GcPauseMilliSeconds = new TimerOptions()
            {
                Context = ContextName,
                Name = "gc_collection_milliseconds",
                DurationUnit = TimeUnit.Milliseconds,
                RateUnit = TimeUnit.Minutes
            };
            
            public static readonly TimerOptions GcCollectionMilliSeconds = new TimerOptions()
            {
                Context = ContextName,
                Name = "gc_pause_milliseconds",
                DurationUnit = TimeUnit.Milliseconds,
                RateUnit = TimeUnit.Minutes
            };
            
            public static readonly TimerOptions ScheduleDelay = new TimerOptions()
            {
                Context = ContextName,
                Name = "threadpool_scheduling_delay_milliseconds",
                DurationUnit = TimeUnit.Milliseconds,
                RateUnit = TimeUnit.Minutes
            };
        }

        public static class Meters
        {
            public static readonly MeterOptions AdjustmentsTotal = new MeterOptions()
            {
                Context = ContextName,
                Name = "threadpool_adjustments",
                MeasurementUnit = Unit.Items
            };
            
            public static readonly MeterOptions AllocatedBytes = new MeterOptions()
            {
                Context = ContextName,
                Name = "gc_allocated_bytes",
                MeasurementUnit = Unit.Bytes
            };

            public static readonly MeterOptions GcCollectionReasons = new MeterOptions()
            {
                Context = ContextName,
                Name = "gc_collection_reasons",
                MeasurementUnit = Unit.Items
            };

            public static readonly MeterOptions ExceptionsThrown = new MeterOptions()
            {
                Context = ContextName,
                Name = "exceptions_thrown",
                MeasurementUnit = Unit.Errors
            };
        }

        public static class Gauges
        {
            public static readonly GaugeOptions GcCpuRatio = new GaugeOptions()
            {
                Context = ContextName,
                Name = "gc_cpu_ratio",
                MeasurementUnit = Unit.None
            };

            public static readonly GaugeOptions GcPauseRatio = new GaugeOptions()
            {
                Context = ContextName,
                Name = "gc_pause_ratio",
                MeasurementUnit = Unit.None
            };
            
            public static readonly GaugeOptions GcHeapSizeBytes = new GaugeOptions()
            {
                Context = ContextName,
                Name = "gc_heap_size_bytes",
                MeasurementUnit = Unit.Bytes
            };
            
            public static readonly GaugeOptions GcNumPinnedObjects = new GaugeOptions()
            {
                Context = ContextName,
                Name = "gc_pinned_objects",
                MeasurementUnit = Unit.Items
            };
            
            public static readonly GaugeOptions GcFinalizationQueueLength = new GaugeOptions()
            {
                Context = ContextName,
                Name = "gc_finalization_queue_length",
                MeasurementUnit = Unit.Items
            };
            
            public static readonly GaugeOptions CpuRatio = new GaugeOptions()
            {
                Context = ContextName,
                Name = "jit_cpu_ratio",
                MeasurementUnit = Unit.None
            };
            
            public static readonly GaugeOptions NumThreads = new GaugeOptions()
            {
                Context = ContextName,
                Name = "threadpool_num_threads",
                MeasurementUnit = Unit.Threads
            };
            
            public static readonly GaugeOptions NumIoThreads = new GaugeOptions()
            {
                Context = ContextName,
                Name = "threadpool_num_io_threads",
                MeasurementUnit = Unit.Threads
            };
        }
    }
}
