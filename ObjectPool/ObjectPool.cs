using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

//In Microsoft.Extensions.ObjectPool(https://source.dot.net/#Microsoft.Extensions.ObjectPool/DefaultObjectPool.cs,3a81fc115b37d15f)
//There is a high performance implemention of lock free object pool,
//copy from them so that could use it in Unity
namespace UselessWheel
{
    public interface IPooledObjectPolicy<T>
    {
        T Create();

        bool Return(T obj);
    }

    public abstract class PooledObjectPolicy<T> : IPooledObjectPolicy<T>
    {
        public abstract T Create();

        public abstract bool Return(T obj);
    }

    public class ObjectPool<T> where T : class
    {
        private protected T _firstItem;
        private protected readonly ObjectWrapper[] _items;
        private protected readonly IPooledObjectPolicy<T> _policy;

        // This class was introduced to avoid the interface call where possible
        private protected readonly PooledObjectPolicy<T> _fastPolicy;

        public ObjectPool(IPooledObjectPolicy<T> policy, int maximumRetained = 0)
        {
            if (maximumRetained <= 0)
            {
                maximumRetained = Environment.ProcessorCount * 2;
            }

            _items = new ObjectWrapper[maximumRetained - 1];
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _fastPolicy = policy as PooledObjectPolicy<T>;
        }

        public T Get()
        {
            var item = _firstItem;
            if (item == null || Interlocked.CompareExchange(ref _firstItem, null, item) != item)
            {
                var items = _items;
                for (var i = 0; i < items.Length; i++)
                {
                    item = items[i].Element;
                    if (item != null && Interlocked.CompareExchange(ref items[i].Element, null, item) == item)
                    {
                        return item;
                    }
                }

                item = Create();
            }

            return item;
        }

        // Non-inline to improve its code quality as uncommon path
        [MethodImpl(MethodImplOptions.NoInlining)]
        private T Create() => _fastPolicy?.Create() ?? _policy.Create();

        public void Return(T obj)
        {
            if (_fastPolicy?.Return(obj) ?? _policy.Return(obj))
            {
                if (_firstItem != null || Interlocked.CompareExchange(ref _firstItem, obj, null) != null)
                {
                    var items = _items;
                    for (var i = 0; i < items.Length && Interlocked.CompareExchange(ref items[i].Element, obj, null) != null; ++i)
                    {
                    }
                }
            }
        }

        // PERF: the struct wrapper avoids array-covariance-checks from the runtime when assigning to elements of the array.
        [DebuggerDisplay("{Element}")]
        private protected struct ObjectWrapper
        {
            public T Element;
        }
    }
}
