using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using UselessWheel;

namespace Benchmark
{
    //FIXME need better benchmark cases
    public class Benchmark
    {
        private class TestClass { }

        public class DefaultPooledObjectPolicy<T> : PooledObjectPolicy<T> where T : class, new()
        {
            public override T Create()
            {
                return new T();
            }

            public override bool Return(T obj)
            {
                return true;
            }
        }

        private readonly ObjectPool<TestClass> objectPool = new ObjectPool<TestClass>(new DefaultPooledObjectPolicy<TestClass>());

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
                        var obj = objectPool.Get();
                        objectPool.Return(obj);
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
