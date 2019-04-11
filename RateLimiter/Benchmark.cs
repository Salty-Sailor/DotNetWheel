using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using UselessWheel;

namespace Benchmark
{
    //FIXME need better benchmark cases
    public class Benchmark
    {
        private RateLimiter ratelimiter;
        private long acquirePermits;

        [IterationSetup]
        public void InterationSetup()
        {
            ratelimiter = new RateLimiter(1000, 100000);
            acquirePermits = 0;
            ratelimiter.Start();
        }

        [IterationCleanup]
        public void InterationCleanup()
        {
            ratelimiter.Stop();
        }

        [Benchmark]
        public void RateLimiterBenchmark()
        {
            var concurrents = 100;
            var waitTasks = new Task[concurrents];
            for (int i = 0; i < concurrents; i++)
            {
                waitTasks[i] = Task.Run(() =>
                {
                    while (true)
                    {
                        if (!ratelimiter.TryAcquirePermit())
                        {
                            Interlocked.Add(ref acquirePermits, 1);
                        }
                        else
                        {
                            return;
                        }
                    }
                });
            }
            Task.WaitAll(waitTasks);
        }

        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<Benchmark>();
        }
    }
}
