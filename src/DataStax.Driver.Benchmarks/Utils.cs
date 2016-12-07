using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataStax.Driver.Benchmarks
{
    public static class Utils
    {
        public static Task Times(long times, int limit, Func<long, Task> method)
        {
            var counter = new SendReceiveCounter();
            var tcs = new TaskCompletionSource<bool>();
            if (limit > times)
            {
                limit = (int)times;
            }
            for (var i = 0; i < limit; i++)
            {
                ExecuteOnceAndContinue(times, method, tcs, counter);
            }
            return tcs.Task;
        }


        private static void ExecuteOnceAndContinue(long times, Func<long, Task> method, TaskCompletionSource<bool> tcs, SendReceiveCounter counter)
        {
            var index = counter.IncrementSent() - 1L;
            if (index >= times)
            {
                return;
            }
            var t1 = method(index);
            t1.ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    tcs.TrySetException(t.Exception.InnerException);
                    return;
                }
                var received = counter.IncrementReceived();
                if (received == times)
                {
                    tcs.TrySetResult(true);
                    return;
                }
                ExecuteOnceAndContinue(times, method, tcs, counter);
            }, TaskContinuationOptions.ExecuteSynchronously);
        }
        
        public class SendReceiveCounter
        {
            private long _receiveCounter;
            private long _sendCounter;

            public long IncrementSent()
            {
                return Interlocked.Increment(ref _sendCounter);
            }

            public long IncrementReceived()
            {
                return Interlocked.Increment(ref _receiveCounter);
            }
        }
    }
}
