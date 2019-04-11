using System;
using System.Threading.Channels;

namespace UselessWheel
{
    public interface IRecyclable
    {
        void Recycle();
    }

    public sealed class ObjectPool<T> where T : IRecyclable
    {
        private readonly Channel<T> pool;
        private readonly Func<T> factoryFunc;

        public ObjectPool(Func<T> objectFactory)
        {
            pool = Channel.CreateBounded<T>(1024);
            factoryFunc = objectFactory;
        }

        public T GetObject()
        {
            if (pool.Reader.TryRead(out var t))
            {
                return t;
            }
            return factoryFunc.Invoke();
        }

        public void PutObject(T item)
        {
            try
            {
                item.Recycle();
                pool.Writer.TryWrite(item);
            }
            catch (Exception) { }
        }
    }
}
