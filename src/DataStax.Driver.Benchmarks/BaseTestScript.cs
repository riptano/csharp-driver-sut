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
        private static readonly Dictionary<int, List<Timer>> ReadGcMetrics = new Dictionary<int, List<Timer>>();
        private static readonly Dictionary<int, List<Timer>> WriteGcMetrics = new Dictionary<int, List<Timer>>();

        public Task Run(Options options)
        {
            Options = options;

            Setup();
            return Task.Factory.StartNew(() => { Start(); }, TaskCreationOptions.LongRunning);
        }

        protected abstract void Setup();

        void GenerateReportLines(Dictionary<int, Metric> metricSeries, List<string> lines, string suffix = "thrpt")
        {
            foreach (var series in metricSeries.Keys)
            {
                var metric = metricSeries[series];

                lines.Add($"{series}");

                foreach (var throughput in metric.Metrics)
                {
                    lines.Add($"{series} {throughput.ToString("F02", CultureInfo.InvariantCulture)}");
                }
                lines.Add($"{series} {suffix}");
            }
        }
        
        void GenerateReportLines(Dictionary<int, List<Timer>> metricSeries, List<string> lines, string suffix = "gcmem")
        {
            foreach (var series in metricSeries.Keys)
            {
                var metrics = metricSeries[series];

                lines.Add($"{series}");

                foreach (var metric in metrics)
                {
                    lines.Add($"{series} {metric.GetMetricsCsvLine()}");
                }
                lines.Add($"{series} {Timer.GetMetricsCsvHeader()} {suffix}");
            }
        }

        private void Report()
        {
            var readReportLines = new List<string>();
            var writeReportLines = new List<string>();
            var readCpuReportLines = new List<string>();
            var readGcReportLines = new List<string>();
            var writeCpuReportLines = new List<string>();
            var writeGcReportLines = new List<string>();
            GenerateReportLines(BaseTestScript.WriteMetrics, writeReportLines);
            GenerateReportLines(BaseTestScript.ReadMetrics, readReportLines);
            GenerateReportLines(BaseTestScript.ReadCpuMetrics, readCpuReportLines, "cpu");
            GenerateReportLines(BaseTestScript.ReadGcMetrics, readGcReportLines, "gcMem");
            GenerateReportLines(BaseTestScript.WriteCpuMetrics, writeCpuReportLines, "cpu");
            GenerateReportLines(BaseTestScript.WriteGcMetrics, writeGcReportLines, "gcMem");
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
            if (readGcReportLines.Count > 0)
            {
                File.WriteAllLines(string.Format(MetricsFilePathFormat, DriverVersion, Options.Profile, Options.Framework, "readgcmem"), readGcReportLines);
            }
            if (writeCpuReportLines.Count > 0)
            {
                File.WriteAllLines(string.Format(MetricsFilePathFormat, DriverVersion, Options.Profile, Options.Framework, "writecpu"), writeCpuReportLines);
            }
            if (writeGcReportLines.Count > 0)
            {
                File.WriteAllLines(string.Format(MetricsFilePathFormat, DriverVersion, Options.Profile, Options.Framework, "writegcmem"), writeGcReportLines);
            }
        }

        public void Start()
        {
            var clients = new int[] { Options.MaxOutstandingRequests };
            if (Options.MaxOutstandingRequests == 0)
            {
                //run all confs
                clients = new[] { 128, 256, 512, 1024 };
            }
            Console.WriteLine("Warming up with 2000 clients...");
            var profile = GetProfile();
            Options.MaxOutstandingRequests = 2000;
            profile.Init(Options).GetAwaiter().GetResult();
            foreach (var numberOfClients in clients)
            {
                BaseTestScript.WriteMetrics.Add(numberOfClients, new Metric());
                BaseTestScript.ReadMetrics.Add(numberOfClients, new Metric());
                BaseTestScript.ReadCpuMetrics.Add(numberOfClients, new Metric());
                BaseTestScript.ReadGcMetrics.Add(numberOfClients, new List<Timer>());
                BaseTestScript.WriteCpuMetrics.Add(numberOfClients, new Metric());
                BaseTestScript.WriteGcMetrics.Add(numberOfClients, new List<Timer>());
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
                RunSingleScriptAsync(profile, Options).GetAwaiter().GetResult();
                Console.WriteLine("Series finished");
                Thread.Sleep(5000);
            }
            Shutdown();
            Report();
        }

        protected abstract void Shutdown();

        private decimal RefreshCpuUsage(Process process, TimeSpan start, ref TimeSpan oldCpuTime, ref long lastMonitorTime)
        {
            var newCpuTime = process.TotalProcessorTime - start;
            var cpuUsage = (newCpuTime - oldCpuTime).TotalSeconds / (Environment.ProcessorCount * ((Stopwatch.GetTimestamp() - lastMonitorTime)/(double)Stopwatch.Frequency));
            lastMonitorTime = Stopwatch.GetTimestamp();
            oldCpuTime = newCpuTime;
            return (decimal)cpuUsage;
        }

        private Task CollectCpuAndGcMetrics(CancellationTokenSource cts, Metric cpuMetrics, Timer gcMetrics)
        {
            return Task.Run(async () =>
            {
                var oldCpuTime = new TimeSpan(0);
                var lastMonitorTime = Stopwatch.GetTimestamp();
                var process = Process.GetCurrentProcess();
                var start = process.TotalProcessorTime;
                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(100, cts.Token).ConfigureAwait(false);
                        gcMetrics.AddLog(GC.GetTotalMemory(false)/1000000);
                    }
                }
                catch (TaskCanceledException)
                {
                }

                process.Refresh();
                cpuMetrics.AddLog(RefreshCpuUsage(process, start, ref oldCpuTime, ref lastMonitorTime));
            });
        }

        private async Task<long> WorkloadTask(Func<Task<Timer>> workload, Options options, Metric metric, Metric cpuMetrics, List<Timer> gcMetrics)
        {
            var workloadWatch = Stopwatch.StartNew();
            for (var i = 0; i < options.Series; i++)
            {
                var timer = new Timer();
                gcMetrics.Add(timer);
                using (var cts = new CancellationTokenSource())
                {
                    var t = CollectCpuAndGcMetrics(cts, cpuMetrics, timer);
                    var watch = Stopwatch.StartNew();
                    var seriesTimer = await workload().ConfigureAwait(false);
                    watch.Stop();
                    cts.Cancel();
                    await t.ConfigureAwait(false);
                    seriesTimer.Count(options.CqlRequests);
                    seriesTimer.TotalTimeInMilliseconds = watch.ElapsedMilliseconds;
                    Console.WriteLine("Finished series: " + (i + 1));
                    metric.AddLog(seriesTimer.GetThroughput());
                    seriesTimer.TimeLogs.Clear();
                }
            }
            workloadWatch.Stop();
            GC.Collect();
            return workloadWatch.ElapsedMilliseconds;
        }

        private async Task RunSingleScriptAsync(IProfile profile, Options options)
        {
            Console.WriteLine("Insert test");
            var totalInsertTime = await WorkloadTask(profile.Insert, options, BaseTestScript.WriteMetrics[Options.MaxOutstandingRequests], BaseTestScript.WriteCpuMetrics[Options.MaxOutstandingRequests], BaseTestScript.WriteGcMetrics[Options.MaxOutstandingRequests]).ConfigureAwait(false);
            Console.WriteLine("--------------------");

            Console.WriteLine("Select test");
            var totalSelectTime = await WorkloadTask(profile.Select, options, BaseTestScript.ReadMetrics[Options.MaxOutstandingRequests], BaseTestScript.ReadCpuMetrics[Options.MaxOutstandingRequests], BaseTestScript.ReadGcMetrics[Options.MaxOutstandingRequests]).ConfigureAwait(false);
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
