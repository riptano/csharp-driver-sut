using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DataStax.Driver.Benchmarks.Metrics;
using DataStax.Driver.Benchmarks.Profiles;
using Timer = DataStax.Driver.Benchmarks.Metrics.Timer;

namespace DataStax.Driver.Benchmarks
{
    abstract class BaseTestScript : ITestScript
    {
        protected static readonly CountRetryPolicy RetryPolicy = new CountRetryPolicy();
        protected Options Options;
        protected string DriverVersion = string.Empty;
        protected string ProfileName = string.Empty;

        protected readonly string MetricsFilePathFormat = "throughput-{0}-{1}-{2}.csv";

        private static readonly Dictionary<int, Metric> WriteMetrics = new Dictionary<int, Metric>();
        private static readonly Dictionary<int, Metric> ReadMetrics = new Dictionary<int, Metric>();

        public Task Run(Options options)
        {
            Options = options;

            Setup();
            return Task.Run(() => { Start(); });
        }

        protected abstract void Setup();

        void GenerateReportLines(Dictionary<int, Metric> metricSeries, List<string> lines)
        {
            foreach (var series in metricSeries.Keys)
            {
                var throughputs = metricSeries[series];

                lines.Add(string.Format("{0}", series));

                foreach (var throughput in throughputs.Metrics)
                {
                    lines.Add(string.Format("{0} {1}", series, throughput.ToString("F02", CultureInfo.InvariantCulture)));
                }
                lines.Add(string.Format("{0} thrpt", series));
            }

        }

        private void Report()
        {
            var readReportLines = new List<string>();
            var writeReportLines = new List<string>();
            GenerateReportLines(WriteMetrics, writeReportLines);
            GenerateReportLines(ReadMetrics, readReportLines);
            if (writeReportLines.Count > 0)
            {
                File.WriteAllLines(string.Format(MetricsFilePathFormat, DriverVersion, Options.Profile, "write"), writeReportLines);
            }
            if (readReportLines.Count > 0)
            {
                File.WriteAllLines(string.Format(MetricsFilePathFormat, DriverVersion, Options.Profile, "read"), readReportLines);
            }
        }

        public void Start()
        {
            var clients = new int[] { Options.MaxOutstandingRequests };
            if (Options.MaxOutstandingRequests == 0)
            {
                //run all confs: 10 20 30 40 50 70 90 110 130 150 180 210 240 270 300 340 380 420 460 500
                clients = new[] { 10, 20, 30, 40, 50, 70, 90, 110, 130, 150, 180, 210, 240, 270, 300, 340, 380, 420, 460, 500 };
            }
            Console.WriteLine("Warming up with 2000 clients...");
            var profile = GetProfile();
            Options.MaxOutstandingRequests = 2000;
            profile.Init(Options).Wait();
            foreach (var numberOfClients in clients)
            {
                WriteMetrics.Add(numberOfClients, new Metric());
                ReadMetrics.Add(numberOfClients, new Metric());
                Options.MaxOutstandingRequests = numberOfClients;
                Console.WriteLine("-----------------------------------------------------");
                Console.WriteLine("Using:");
                Console.WriteLine("- Driver " + DriverVersion);
                Console.WriteLine("- Connections per hosts " + Options.ConnectionsPerHost);
                Console.WriteLine("- Max outstanding requests " + Options.MaxOutstandingRequests);
                Console.WriteLine("- Operations per series " + Options.CqlRequests);
                Console.WriteLine("- Series count: " + Options.Series);
                ProfileName = profile.GetType().GetTypeInfo().Name;
                Console.WriteLine("- Using \"{0}\" profile", ProfileName);
                Console.WriteLine("-----------------------------------------------------");
                RunSingleScriptAsync(profile, Options).Wait();
                Console.WriteLine("Series finished");
                Thread.Sleep(5000);
            }
            Shutdown();
            Report();
        }

        protected abstract void Shutdown();

        private async Task<long> WorkloadTask(Func<Task<Timer>> workload, Options options, Metric metric)
        {
            var workloadWatch = Stopwatch.StartNew();
            for (var i = 0; i < options.Series; i++)
            {
                var watch = Stopwatch.StartNew();
                var seriesTimer = await workload();
                watch.Stop();
                seriesTimer.Count(options.CqlRequests);
                seriesTimer.TotalTimeInMilliseconds = watch.ElapsedMilliseconds;
                Console.WriteLine("Finished series: " + (i + 1));
                metric.AddLog(seriesTimer.GetThroughput());
                seriesTimer.TimeLogs.Clear();
            }
            workloadWatch.Stop();
            GC.Collect();
            return workloadWatch.ElapsedMilliseconds;
        }

        private async Task RunSingleScriptAsync(IProfile profile, Options options)
        {
            Console.WriteLine("Insert test");
            var totalInsertTime = await WorkloadTask(profile.Insert, options, WriteMetrics[Options.MaxOutstandingRequests]);
            Console.WriteLine("--------------------");

            Console.WriteLine("Select test");
            var totalSelectTime = await WorkloadTask(profile.Select, options, ReadMetrics[Options.MaxOutstandingRequests]);
            Console.WriteLine(
                "______________________________________\n" +
                "|      Insert      |       Select    |\n" +
                "|------------------|-----------------|\n" +
                "|      {0:000000}      |       {1:000000}    |\n" +
                "|------------------------------------|\n",
                1000D * options.CqlRequests / (totalInsertTime / options.Series),
                1000D * options.CqlRequests / (totalSelectTime / options.Series));

            // Show results
            Console.WriteLine("Errors: {0} read timeouts, {1} write timeouts and {2} unavailable exceptions",
                RetryPolicy.GetReadCount(), RetryPolicy.GetWriteCount(), RetryPolicy.GetUnavailableCount());
        }

        protected abstract IProfile GetProfile();
    }
}
