using System;
using System.Threading;

namespace UselessWheel
{
    public class RateLimiter
    {
        private readonly int permitsPerSecond; 
        private readonly int maxStorePermits;
        private readonly object tokenLock = new object();
        private Timer timer;
        private long currentPermitsCount;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="permitsPerSecond"></param>
        /// <param name="maxStorePermits">the max </param>
        public RateLimiter(int permitsPerSecond = 20, int maxStorePermits = 500)
        {
            if (permitsPerSecond <= 0 || maxStorePermits <= 0)
            {
                throw new ArgumentException("Invalid Arguement");
            }
            this.permitsPerSecond = permitsPerSecond;
            this.maxStorePermits = maxStorePermits;
            currentPermitsCount = maxStorePermits;
        }

        public void Start()
        {
            timer = new Timer(addOnePermit, null, 1000 / permitsPerSecond, 1000 / permitsPerSecond);
        }

        public void Stop()
        {
            timer.Dispose();
            Interlocked.Exchange(ref currentPermitsCount, 0);
        }

        public bool TryAcquirePermit()
        {
            while (true)
            {
                var a = Interlocked.Read(ref currentPermitsCount);
                if (a <= 0)
                {
                    Interlocked.Exchange(ref currentPermitsCount, 0);
                    return false;
                }

                if (a == Interlocked.CompareExchange(ref currentPermitsCount, a - 1, a))
                {
                    return true;
                }
            }
        }

        private void addOnePermit(object obj)
        {
            while (true)
            {
                var a = Interlocked.Read(ref currentPermitsCount);
                if (a >= maxStorePermits)
                {
                    Interlocked.Exchange(ref currentPermitsCount, maxStorePermits);
                    return;
                }

                if (a == Interlocked.CompareExchange(ref currentPermitsCount, a + 1, a))
                {
                    return;
                }
            }
        }
    }
}
