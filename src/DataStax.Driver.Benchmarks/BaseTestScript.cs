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

        protected readonly string MetricsFilePathFormat = "throughput-{0}-{1}-{2}-{3}.csv";

        private static readonly Dictionary<int, Metric> WriteMetrics = new Dictionary<int, Metric>();
        private static readonly Dictionary<int, Metric> ReadMetrics = new Dictionary<int, Metric>();
        private static readonly Dictionary<int, Metric> ReadCpuMetrics = new Dictionary<int, Metric>();
        private static readonly Dictionary<int, Metric> WriteCpuMetrics = new Dictionary<int, Metric>();

        public Task Run(Options options)
        {
            Options = options;

            Setup();
            return Task.Factory.StartNew(() => { Start(); }, TaskCreationOptions.LongRunning);
        }

        protected abstract void Setup();

        void GenerateReportLines(Dictionary<int, Metric> metricSeries, List<string> lines)
        {
            foreach (var series in metricSeries.Keys)
            {
                var metric = metricSeries[series];
                
                foreach (var throughput in metric.Metrics)
                {
                    lines.Add($"{series} {throughput.ToString("F02", CultureInfo.InvariantCulture)}");
                }
            }
        }
        
        void GenerateReportLines(Dictionary<int, List<Timer>> metricSeries, List<string> lines)
        {
            foreach (var series in metricSeries.Keys)
            {
                var metrics = metricSeries[series];
                
                foreach (var metric in metrics)
                {
                    lines.Add($"{series} {metric.GetMetricsCsvLine()}");
                }
            }
        }

        private void Report()
        {
            var readReportLines = new List<string>();
            var writeReportLines = new List<string>();
            var readCpuReportLines = new List<string>();
            var writeCpuReportLines = new List<string>();
            GenerateReportLines(BaseTestScript.WriteMetrics, writeReportLines);
            GenerateReportLines(BaseTestScript.ReadMetrics, readReportLines);
            GenerateReportLines(BaseTestScript.ReadCpuMetrics, readCpuReportLines);
            GenerateReportLines(BaseTestScript.WriteCpuMetrics, writeCpuReportLines);
            if (writeReportLines.Count > 0)
            {
                File.WriteAllLines(string.Format(MetricsFilePathFormat, DriverVersion, Options.Profile, Options.Framework, "write"), writeReportLines);
            }
            if (readReportLines.Count > 0)
            {
                File.WriteAllLines(string.Format(MetricsFilePathFormat, DriverVersion, Options.Profile, Options.Framework, "read"), readReportLines);
            }
            if (readCpuReportLines.Count > 0)
            {
                File.WriteAllLines(string.Format(MetricsFilePathFormat, DriverVersion, Options.Profile, Options.Framework, "readcpu"), readCpuReportLines);
            }
            if (writeCpuReportLines.Count > 0)
            {
                File.WriteAllLines(string.Format(MetricsFilePathFormat, DriverVersion, Options.Profile, Options.Framework, "writecpu"), writeCpuReportLines);
            }
        }

        public void Start()
        {
            var clients = Options.MaxOutstandingRequestsStr.Split(',').Select(int.Parse).ToArray();
            Console.WriteLine("Warming up with 2000 clients...");
            Options.CurrentOutstandingRequests = 2000;
            var profile = GetProfile();
            profile.Init(Options).GetAwaiter().GetResult();
            foreach (var numberOfClients in clients)
            {
                BaseTestScript.WriteMetrics.Add(numberOfClients, new Metric());
                BaseTestScript.ReadMetrics.Add(numberOfClients, new Metric());
                BaseTestScript.ReadCpuMetrics.Add(numberOfClients, new Metric());
                BaseTestScript.WriteCpuMetrics.Add(numberOfClients, new Metric());
                Options.CurrentOutstandingRequests = numberOfClients;
                Console.WriteLine("-----------------------------------------------------");
                Console.WriteLine("Using:");
                Console.WriteLine("- Driver " + DriverVersion);
                Console.WriteLine("- Connections per hosts " + Options.ConnectionsPerHost);
                Console.WriteLine("- Socket Stream Mode: " + Options.StreamMode);
                Console.WriteLine("- Max outstanding requests " + Options.CurrentOutstandingRequests);
                Console.WriteLine("- Operations per series " + Options.CqlRequests);
                Console.WriteLine("- Series count: " + Options.Series);
                ProfileName = profile.GetType().GetTypeInfo().Name;
                Console.WriteLine("- Using \"{0}\" profile", ProfileName);
                Console.WriteLine("-----------------------------------------------------");
                RunSingleScriptAsync(profile, Options).GetAwaiter().GetResult();
                Console.WriteLine("Series finished");
            }
            Shutdown();
            Report();
        }

        protected abstract void Shutdown();
        
        private async Task<long> WorkloadTask(Func<Task<Timer>> workload, Options options, Metric metric, Metric cpuMetrics)
        {
            var totalMilliseconds = 0L;

            for (var i = 0; i < options.Series; i++)
            {
                var startTime = DateTime.UtcNow;
                var start = Process.GetCurrentProcess().TotalProcessorTime;

                var initialTimestamp = Stopwatch.GetTimestamp();

                var seriesTimer = await workload().ConfigureAwait(false);

                var finalTimestamp = Stopwatch.GetTimestamp();

                var elapsedSeconds = (finalTimestamp - initialTimestamp) / (double)Stopwatch.Frequency;

                var endTime = DateTime.UtcNow;
                var end = Process.GetCurrentProcess().TotalProcessorTime;
                var cpuUsage = ((end - start).TotalMilliseconds / Environment.ProcessorCount) 
                               / (endTime - startTime).TotalMilliseconds;

                var finalCpuUsage = (decimal)cpuUsage;
                cpuMetrics.AddLog(finalCpuUsage);

                seriesTimer.Count(options.CqlRequests);
                seriesTimer.TotalTimeInMilliseconds = (long)(elapsedSeconds * 1000);
                Console.WriteLine("Finished series: " + (i + 1));
                metric.AddLog(seriesTimer.GetThroughput());
                seriesTimer.TimeLogs.Clear();

                totalMilliseconds += seriesTimer.TotalTimeInMilliseconds;
            }
            
            return totalMilliseconds;
        }

        private async Task RunSingleScriptAsync(IProfile profile, Options options)
        {
            Console.WriteLine("Insert test");
            var totalInsertTime = await WorkloadTask(profile.Insert, options, BaseTestScript.WriteMetrics[Options.CurrentOutstandingRequests], BaseTestScript.WriteCpuMetrics[Options.CurrentOutstandingRequests]).ConfigureAwait(false);
            Console.WriteLine("--------------------");

            Console.WriteLine("Select test");
            var totalSelectTime = await WorkloadTask(profile.Select, options, BaseTestScript.ReadMetrics[Options.CurrentOutstandingRequests], BaseTestScript.ReadCpuMetrics[Options.CurrentOutstandingRequests]).ConfigureAwait(false);
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
                BaseTestScript.RetryPolicy.GetReadCount(), BaseTestScript.RetryPolicy.GetWriteCount(), BaseTestScript.RetryPolicy.GetUnavailableCount());
        }

        protected abstract IProfile GetProfile();
    }
}
