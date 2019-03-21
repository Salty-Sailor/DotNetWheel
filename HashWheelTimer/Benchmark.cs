using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace UselessWheel
{
    public class Benchmark
    {
        private HashedWheelTimer hashedWheelTimer;
        private static long counter;
        private static ManualResetEvent mutex;
        private readonly long scheduleTimes = 10000;
        private readonly int threads = 100;

        [IterationSetup]
        public void InterationSetup()
        {
            hashedWheelTimer = new HashedWheelTimer(1, 10000);

            counter = 0;
            mutex = new ManualResetEvent(false);
        }

        [IterationCleanup]
        public void InterationCleanup()
        {
            hashedWheelTimer.Stop();
        }

        [Benchmark]
        public void HashWheelTimerBenchmark()
        {
            var iterTimes = scheduleTimes / threads;
            for (int i = 0; i < threads; i++)
            {
                Task.Run(() =>
                {
                    for (int j = 0; j < iterTimes; j++)
                    {
                        hashedWheelTimer.Schedule(testWork, j);
                    }
                });
            }

            mutex.WaitOne();
        }

        private void testWork()
        {
            if (Interlocked.Read(ref counter) == ( scheduleTimes - 1 ))
            {
                mutex.Set();
                return;
            }
            Interlocked.Add(ref counter, 1);
        }


        public static void Main()
        {
            BenchmarkRunner.Run<Benchmark>();
        }
    }
}
