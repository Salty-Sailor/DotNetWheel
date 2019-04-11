using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using UselessWheel;

namespace Benchmark
{
    //FIXME need better benchmark cases
    public class Benchmark
    {
        private class TestClass : IRecyclable
        {
            public void Recycle()
            {
            }
        }

        private readonly ObjectPool<TestClass> oPool = new ObjectPool<TestClass>(() => new TestClass());

        [Benchmark]
        public void ObjectPoolBenchmark()
        {
            var concurrents = 100;
            var iterationTimes = 10000;
            var waitTasks = new Task[concurrents];
            for (int i = 0; i < concurrents; i++)
            {
                waitTasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < iterationTimes; j++)
                    {
                        var obj = oPool.GetObject();
                        oPool.PutObject(obj);
                    }
                });
            }
            Task.WaitAll(waitTasks);
        }

        public static void Main()
        {
            BenchmarkRunner.Run<Benchmark>();
        }
    }
}
