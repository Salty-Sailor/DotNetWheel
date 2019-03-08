using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace HashWheelTimer
{
    public class HashWheelTimer
    {
        private int tickDuration;//ms
        private readonly int maxTimeout;
        private readonly int wheelSize;
        private long currentIndex = 0;
        private readonly object[] slotLock;
        private Queue<Action>[] slot;
        private Stopwatch sw = new Stopwatch();
        private CancellationTokenSource cts;
        private CancellationToken cToken;

        public HashWheelTimer(int duration = 50, int maxto = 5000)
        {
            Debug.Assert(duration > 0);
            Debug.Assert(maxto > 0 && maxto >= duration);

            cts = new CancellationTokenSource();
            cToken = cts.Token;

            tickDuration = duration;
            maxTimeout = maxto;

            if (maxTimeout % duration == 0)
            {
                wheelSize = maxTimeout / duration;
            }
            else
            {
                wheelSize = ( maxTimeout / duration ) + 1;
            }

            slot = new Queue<Action>[wheelSize];
            slotLock = new object[wheelSize];
            for (int i = 0; i < wheelSize; i++)
            {
                slot[i] = new Queue<Action>();
                slotLock[i] = new object();
            }

            Task.Run(async () =>
            {
                sw.Start();
                var execCount = 0;
                while (true)
                {
                    if (( sw.ElapsedMilliseconds / tickDuration ) > execCount)
                    {
                        doWork();
                        execCount++;
                    }
                    else
                    {
                        await Task.Delay(tickDuration);
                    }
                }
            }, cToken);
        }

        public void Stop()
        {
            cts.Cancel();
        }

        public void Schedule(Action cb, int delayTime)
        {
            if (delayTime > maxTimeout)
            {
                throw new Exception($"schedule timeout:{delayTime} larger than maxTimeout:{maxTimeout}");
            }

            int timeOffset = calOffset(delayTime);
            var cur = (int)Interlocked.Read(ref currentIndex);
            int scheduleIndex = ( cur + timeOffset ) % wheelSize;

            lock (slotLock[scheduleIndex])
            {
                slot[scheduleIndex].Enqueue(cb);
            }
        }

        private void doWork()
        {
            currentIndex = ( ++currentIndex ) % wheelSize;
            lock (slotLock[currentIndex])
            {
                while (slot[currentIndex].Count > 0)
                {
                    slot[currentIndex].Dequeue().Invoke();
                }
            }
        }

        private int calOffset(int time)
        {
            if (time % tickDuration == 0 && time != 0)
            {
                return time / tickDuration;
            }

            return ( time / tickDuration ) + 1;
        }
    }
}
