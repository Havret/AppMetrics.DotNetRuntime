using System;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics.DotNetRuntime;
using App.Metrics.DotNetRuntime.StatsCollectors.Util;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Perfolizer.Horology;


namespace Benchmarks
{
    public class Program
    {
        static void Main(string[] args)
        {
            var tasks = Enumerable.Range(1, 2_000_000)
                .Select(_ => Task.Run(() => 1))
                .ToArray();

            var b = new byte[1024 * 1000];
            if (args.Length > 0 && args[0] == "metrics")
            {
                var metrics = App.Metrics.AppMetrics.CreateDefaultBuilder().Build();
                var collector = DotNetRuntimeStatsBuilder.Customize()
                    .WithContentionStats()
                    .WithThreadPoolStats()
                    .WithThreadPoolSchedulingStats();
                
                if (args.Any(x => x == "jit"))
                    collector.WithJitStats();
                if(args.Any(x => x=="gc"))
                    collector.WithGcStats();
                        
                collector
                    .WithDebuggingMetrics(false)
                    .StartCollecting(metrics);
            }

            var b2 = new byte[1024 * 1000];
            var b3 = new byte[1024 * 1000];

            Task.WaitAll(tasks);

            Console.WriteLine("Done");

           // return;
            BenchmarkRunner.Run<TestBenchmark>(
                DefaultConfig.Instance
                    .With(
                        Job.Default
                            .With(CoreRuntime.Core31)
                            .WithLaunchCount(1)
                            .WithIterationTime(TimeInterval.FromMilliseconds(200))
                            .With(Platform.X64)
                            .With(Jit.RyuJit)
                    )
            );
        }
     
        public class TestBenchmark
        {
            private TimeSpan t1 = TimeSpan.FromSeconds(1);
            private TimeSpan t2 = TimeSpan.FromSeconds(60);
            private long l1 = 1l;
            private long l2 = 60;
	
            [Benchmark]
            public TimeSpan TestAddition() => t1 + t2;


            [Benchmark]
            [MethodImpl(MethodImplOptions.NoOptimization)]
            public long TestAdditionLong() => l1 + l2;

            [Benchmark]
            [MethodImpl(MethodImplOptions.NoOptimization)]
            public long TestInterlockedIncLong() => Interlocked.Increment(ref l1);
	
            private EventPairTimer<int> timer = new EventPairTimer<int>(1, 2, x => x.EventId);

            private EventWrittenEventArgs eventArgs;

            public TestBenchmark()
            {
                eventArgs = (EventWrittenEventArgs)Activator.CreateInstance(typeof(EventWrittenEventArgs), BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance, (Binder) null, new object[] {null}, null);
            }
          
        }
    }
}