using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;

namespace DataStax.Driver.Benchmarks.Metrics
{
    public class Timer
    {
        private LinkedList<long> _timeLogs;

        public LinkedList<long> TimeLogs
        {
            get
            {
                return _timeLogs;
            }
        }

        private Semaphore _semaphore;

        private int _lastCount = 0;

        public long TotalTimeInMilliseconds;

        public Timer()
        {
            _timeLogs = new LinkedList<long>();
            _semaphore = new Semaphore(1, 1);
        }

        public void AddLog(long timeLog)
        {
            _semaphore.WaitOne();
            TimeLogs.AddLast(timeLog);
            _semaphore.Release();
        }

        public int Count()
        {
            _semaphore.WaitOne();
            _lastCount = TimeLogs.Count;
            _semaphore.Release();
            return _lastCount;
        }

        public void Count(int count)
        {
            _lastCount = count;
        }

        public void Clear()
        {
            _semaphore.WaitOne();
            for (var count = 0; count < _lastCount; count++)
            {
                TimeLogs.RemoveFirst();
            }
            _lastCount = 0;
            _semaphore.Release();
        }

        public TimeLogRecorder NewContext()
        {
            return new TimeLogRecorder(this);
        }

        public class TimeLogRecorder : IDisposable
        {
            private readonly Timer _timer;
            private Stopwatch _stopWatch;

            public TimeLogRecorder(Timer timer)
            {
                _timer = timer;
                _stopWatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                StopRecording();
                _stopWatch = null;
            }

            public void StopRecording()
            {
                if (!_stopWatch.IsRunning)
                {
                    return;
                }
                _stopWatch.Stop();
                _timer.AddLog(_stopWatch.ElapsedMilliseconds);
            }
        }

        public static Dictionary<int, double> GetPercentiles(int[] percentiles, long[] copiedLogs)
        {
            var result = new Dictionary<int, double>();
            if (copiedLogs.Length == 0)
                return result;
            Array.Sort(copiedLogs, (log, timeLog) => (int) (log - timeLog));

            foreach (var percentile in percentiles)
            {
                var percentileResult = 0.0;
                var relativeIndex = (double) copiedLogs.Length*percentile/100;
                if (relativeIndex % 1 == 0.0)
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

        public static string GetMetricsCsvHeader()
        {
            return "min,25,50,75,95,98,99,max,mean,count,time,thrpt";
        }

        public string GetMetricsCsvLine()
        {
            if (TimeLogs.Count == 0)
            {
                return string.Empty;
            }
            var buffer = new StringBuilder();
            var length = this.Count();
            var copiedLogs = new long[length];
            var timeLog = TimeLogs.First;

            for (int i = 0; i < length; i++)
            {
                copiedLogs[i] = timeLog.Value;
                timeLog = timeLog.Next;
            }

            var percentiles = GetPercentiles(new int[] { 25, 50, 75, 95, 98, 99 }, copiedLogs);

            var min = string.Format("{0},", TimeLogs.Min());
            buffer.Append(min);

            foreach (var percentile in percentiles.Keys)
            {
                var toSend = string.Format("{0},", percentiles[percentile]);
                buffer.Append(toSend);
            }
            var max = string.Format("{0},", TimeLogs.Max());
            buffer.Append(max);

            var mean = string.Format("{0},", ((decimal) TimeLogs.Sum() / TimeLogs.Count).ToString("F02", CultureInfo.InvariantCulture));
            buffer.Append(mean);

            //count
            var count = string.Format("{0},", TimeLogs.Count);
            buffer.Append(count);

            //count
            var totalTime = string.Format("{0},", TotalTimeInMilliseconds);
            buffer.Append(totalTime);

            //thrpt
            var thrpt = string.Format("{0}", GetThroughput().ToString("F02", CultureInfo.InvariantCulture));
            buffer.Append(thrpt);

            return buffer.ToString();
        }

        public decimal GetThroughput()
        {
            if (TotalTimeInMilliseconds == 0)
                return 0;
            return 1000 * (decimal) _lastCount / TotalTimeInMilliseconds;
        }
    }
}
