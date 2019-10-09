using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataStax.Driver.Benchmarks
{
    public static class Utils
    {
        public static async Task RunMultipleThreadsAsync(long times, int limit, Func<long, Task> method)
        {
            if (limit > times)
            {
                limit = (int)times;
            }

            var perThread = times / (double)limit;
            var perThreadInt = (int)perThread;
            var remainder = times - (perThreadInt * limit);
            var tasks = new List<Task>(limit);
            long lastMaxIndex = 0;
            long totalCount = 0;
            for (var i = 0; i < limit; i++)
            {
                var currentIndex = lastMaxIndex;
                var maxIndex = currentIndex + perThreadInt;
                if (remainder > 0)
                {
                    maxIndex++;
                    remainder--;
                }

                lastMaxIndex = maxIndex;
                tasks.Add(Task.Run(async () =>
                {
                    for (var j = currentIndex; j < maxIndex; j++)
                    {
                        await method(j).ConfigureAwait(false);
                    }
                }));

                totalCount += (maxIndex - currentIndex);
            }

            Console.WriteLine("Count: " + totalCount + ". Expected: " + times);

            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException ?? ex.InnerExceptions.FirstOrDefault() ?? ex;
            }
        }
    }
}