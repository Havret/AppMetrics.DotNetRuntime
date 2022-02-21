using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.DotNetRuntime;
using App.Metrics.DotNetRuntime.StatsCollectors;
using NUnit.Framework;

namespace AppMetrics.DotNetRuntime.Tests.StatsCollectors.IntegrationTests
{
    internal class ThreadPoolStatsCollectorTests : StatsCollectorIntegrationTestBase<ThreadPoolStatsCollector>
    {
        protected override ThreadPoolStatsCollector CreateStatsCollector()
        {
            return new ThreadPoolStatsCollector(MetricsClient);
        }
        
        [Test]
        public async Task When_work_is_executed_on_the_thread_pool_then_executed_work_is_measured()
        {
            MetricsClient.Provider.Meter.Instance(DotNetRuntimeMetricsRegistry.Meters.AdjustmentsTotal, new MetricTags("reason", "climbing_move")).Reset();
            MetricsClient.Provider.Gauge.Instance(DotNetRuntimeMetricsRegistry.Gauges.NumThreads).Reset();
            // schedule a bunch of blocking tasks that will make the thread pool will grow
            var tasks = Enumerable.Range(1, 1000)
                .Select(_ => Task.Run(() => Thread.Sleep(20)));

            await Task.WhenAll(tasks);

            Assert.That(() => GetGauage(DotNetRuntimeMetricsRegistry.Gauges.NumThreads.Name).Value, 
                Is.GreaterThanOrEqualTo(Environment.ProcessorCount).After(2000, 10));
            Assert.That(() => GetMeter(DotNetRuntimeMetricsRegistry.Meters.AdjustmentsTotal.Name, "reason:climbing_move").Value.Count,
                Is.GreaterThanOrEqualTo(1).After(2000 + (int) (2000 * 0.1D), 10));
        }
        
        [Test]
        [Ignore("ETW events are not being triggered for io threads, need to figure out why")]
        public async Task When_IO_work_is_executed_on_the_thread_pool_then_the_number_of_io_threads_is_measured()
        {
            MetricsClient.Provider.Gauge.Instance(DotNetRuntimeMetricsRegistry.Gauges.NumIoThreads).Reset();
            
            // need to schedule a bunch of IO work to make the IO pool grow
            using (var client = new HttpClient())
            {
                var httpTasks = Enumerable.Range(1, 50)
                    .Select(_ => client.GetAsync("http://google.com"));

                await Task.WhenAll(httpTasks);
            }
            
            Assert.That(() => GetGauage(DotNetRuntimeMetricsRegistry.Gauges.NumIoThreads.Name).Value,
                Is.GreaterThanOrEqualTo(1).After(2000, 10));
        }
    }
}