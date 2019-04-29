using System;
using System.Threading;
using System.Threading.Tasks;

namespace UselessWheel
{
    /// <summary>
    /// UnixTime class only provide an inaccurate timestamp,
    /// If you need more accuracy, consider to use System.Diagnostics.Stopwatch
    /// </summary>
    public static class UnixTime
    {
        private static long second;

        public static long Second
        {
            get => Interlocked.Read(ref second);
        }

        public static long Millisecond
        {
            get => (long)( DateTime.UtcNow - DateTime.UnixEpoch ).TotalMilliseconds;
        }

        static UnixTime()
        {
            second = (long)( DateTime.UtcNow - DateTime.UnixEpoch ).TotalSeconds;

            Task.Factory.StartNew(async () =>
            {
                var sleepTime = new TimeSpan(0, 0, 1);
                while (true)
                {
                    await Task.Delay(sleepTime);
                    Interlocked.Exchange(ref second, (long)( DateTime.UtcNow - DateTime.UnixEpoch ).TotalSeconds);
                }
            });
        }
    }
}
