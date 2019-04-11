using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace UselessWheel
{
    public class HashedWheelTimer
    {
        private readonly int tickDuration;//ms
        private readonly int maxTimeout;
        private readonly int size;
        private int currentIndex = 0;
        private readonly object slotLock = new object();
        private readonly Queue<Action>[] wheelBuckets;
        private readonly Stopwatch sw = new Stopwatch();
        private readonly CancellationTokenSource cancel;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="duration">time accuracy</param>
        /// <param name="maxto">max delay time in this timerwheel</param>
        public HashedWheelTimer(int duration = 50, int maxto = 5000)
        {
            Debug.Assert(duration > 0);
            Debug.Assert(maxto > 0 && maxto >= duration);

            cancel = new CancellationTokenSource();
            tickDuration = duration;
            maxTimeout = maxto;

            if (maxTimeout % duration == 0)
            {
                size = maxTimeout / duration;
            }
            else
            {
                size = maxTimeout / duration + 1;
            }

            wheelBuckets = new Queue<Action>[size];
            for (int i = 0; i < size; i++)
            {
                wheelBuckets[i] = new Queue<Action>();
            }

            var t = new Task(async () =>
            {
                sw.Start();
                var execCount = 0;
                while (true)
                {
                    if (( sw.ElapsedMilliseconds / tickDuration ) > execCount)
                    {
                        fire();
                        execCount++;
                    }
                    else
                    {
                        await Task.Delay(tickDuration);
                    }
                }
            }, cancel.Token);
            t.Start();
        }

        public void Stop()
        {
            cancel.Cancel();
        }

        public void Schedule(Action cb, int delayTime)
        {
            if (delayTime > maxTimeout || delayTime < 0)
            {
                throw new Exception($"schedule timeout:{delayTime} Invalid");
            }

            int timeOffset = calculateOffset(delayTime);
            lock (slotLock)
            {
                int bucketIndex = ( currentIndex + timeOffset ) % size;
                wheelBuckets[bucketIndex].Enqueue(cb);
            }
        }

        private void fire()
        {
            lock (slotLock)
            {
                currentIndex = ( ++currentIndex ) % size;
                while (wheelBuckets[currentIndex].Count > 0)
                {
                    try
                    {
                        wheelBuckets[currentIndex].Dequeue().Invoke();
                    }
                    catch (Exception) { }
                }
            }
        }

        private int calculateOffset(int time)
        {
            if (time % tickDuration == 0 && time != 0)
            {
                return time / tickDuration;
            }

            return time / tickDuration + 1;
        }
    }
}
