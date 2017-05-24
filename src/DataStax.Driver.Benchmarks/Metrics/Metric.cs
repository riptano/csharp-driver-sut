using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DataStax.Driver.Benchmarks.Metrics
{
    public class Metric
    {
        private LinkedList<decimal> _metrics;
        public LinkedList<decimal> Metrics
        {
            get { return _metrics; }
        }

        private Semaphore _semaphore;

        private int _lastCount = 0;

        public Metric()
        {
            _metrics = new LinkedList<decimal>();
            _semaphore = new Semaphore(1, 1);
        }

        public void AddLog(Decimal metric)
        {
            _semaphore.WaitOne();
            Metrics.AddLast(metric);
            _semaphore.Release();
        }

        public int Count()
        {
            _semaphore.WaitOne();
            _lastCount = Metrics.Count;
            _semaphore.Release();
            return _lastCount;
        }

        public void Clear()
        {
            _semaphore.WaitOne();
            for (var count = 0; count < _lastCount; count++)
            {
                Metrics.RemoveFirst();
            }
            _lastCount = 0;
            _semaphore.Release();
        }

        public static Dictionary<int, decimal> GetPercentiles(int[] percentiles, decimal[] copiedLogs)
        {
            var result = new Dictionary<int, decimal>();
            if (copiedLogs.Length == 0)
                return result;
            Array.Sort(copiedLogs, (log, timeLog) => (int) (log - timeLog));

            foreach (var percentile in percentiles)
            {
                decimal percentileResult = 0;
                var relativeIndex = (decimal) copiedLogs.Length*percentile/100;
                if (relativeIndex % 1 == 0)
                {
                    var index = (int) relativeIndex - 1;
                    if (index >= copiedLogs.Length)
                        index = copiedLogs.Length - 1;
                    if (index < 0)
                        index = 1;
                    percentileResult = (index + 1) < copiedLogs.Length
                        ? (copiedLogs[index] + copiedLogs[index + 1]) / 2
                        : copiedLogs[index];
                }
                else
                {
                    var index = (int) Math.Ceiling(relativeIndex) - 1;
                    if (index >= copiedLogs.Length)
                        index = copiedLogs.Length - 1;
                    if (index < 0)
                        index = 1;
                    percentileResult = copiedLogs[index];
                }
                result.Add(percentile, percentileResult);
            }
            return result;
        }
        
    }
}
