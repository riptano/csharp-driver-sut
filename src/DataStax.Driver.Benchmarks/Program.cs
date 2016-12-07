using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cassandra;
using CommandLine;
using DataStax.Driver.Benchmarks.Models;
using DataStax.Driver.Benchmarks.Profiles;
using Microsoft.Owin.Hosting;

namespace DataStax.Driver.Benchmarks
{
    public class Program
    {
        private static readonly CountRetryPolicy RetryPolicy = new CountRetryPolicy();
       

        static void Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<Options>(args);
            var options = result.MapResult(o => o, e => null);
            if (options == null)
            {
                return;
            }
            Diagnostics.CassandraTraceSwitch.Level = options.Debug ? TraceLevel.Info : TraceLevel.Warning;
            Trace.Listeners.Add(new ConsoleTraceListener());
            var driverVersion = Version.Parse(FileVersionInfo.GetVersionInfo(
                Assembly.GetAssembly(typeof (ISession)).Location).FileVersion);
            Console.WriteLine("Using driver version {0}", driverVersion);
            var cluster = Cluster.Builder()
                .AddContactPoint(options.ContactPoint)
                .WithSocketOptions(new SocketOptions().SetTcpNoDelay(true).SetReadTimeoutMillis(0))
                .WithRetryPolicy(RetryPolicy)
                .WithPoolingOptions(new PoolingOptions()
                    .SetCoreConnectionsPerHost(HostDistance.Local, options.ConnectionsPerHost)
                    .SetMaxConnectionsPerHost(HostDistance.Local, options.ConnectionsPerHost)
                    .SetMaxSimultaneousRequestsPerConnectionTreshold(HostDistance.Local, 2048))
                .WithQueryOptions(new QueryOptions().SetConsistencyLevel(ConsistencyLevel.LocalOne))
                .Build();
            var session = cluster.Connect();
            if (options.UseHttp.ToString().ToUpperInvariant() != "Y")
            {
                Console.WriteLine("Using single script");
                SingleScript(session, options);
                return;
            }
            Console.WriteLine("Starting benchmarks web server...");
            var metricsHost = options.MetricsEndpoint.Split(':');
            var metrics = new MetricsTracker(new IPEndPoint(IPAddress.Parse(metricsHost[0]), Convert.ToInt32(metricsHost[1])), driverVersion);
            var repository = new Repository(session, metrics, true, options);
            using (WebApp.Start(options.Url, b => WebStartup.Build(repository, b)))
            {
                Console.WriteLine("Server running on " + options.Url);
                Console.ReadLine();
            }
            metrics.Dispose();
            cluster.Shutdown(3000);
        }

        private static void SingleScript(ISession session, Options options)
        {
            Task.Run(async () =>
            {
                await RunSingleScriptAsync(session, options);
            }).Wait();
        }

        private static async Task RunSingleScriptAsync(ISession session, Options options)
        {
            IProfile profile;
            if (options.Profile == "minimal")
            {
                profile = new MinimalProfile();
            }
            else
            {
                profile = new StandardProfile();
            }
            Console.WriteLine("Using \"{0}\" profile", profile.GetType().GetTypeInfo().Name);
            await profile.Init(session, options);
            if (options.Debug)
            {
                Console.WriteLine("Starting Insert");
            }
            var elapsedInsert = await WorkloadTask(profile.Insert, options);
            if (options.Debug)
            {
                Console.WriteLine("Starting Select");
            }
            var elapsedSelect = await WorkloadTask(profile.Select, options);
            // Show results
            Console.WriteLine("Errors: {0} read timeouts, {1} write timeouts and {2} unavailable exceptions", 
                RetryPolicy.GetReadCount(), RetryPolicy.GetWriteCount(), RetryPolicy.GetUnavailableCount());
            Console.WriteLine("Throughput:");
            Console.WriteLine(
                "|      Insert      |       Select    |\n" +
                "|------------------|-----------------|\n" +
                "|      {0:000000}      |       {1:000000}    |", 
                1000D * options.CqlRequests / elapsedInsert.Average(),
                1000D * options.CqlRequests / elapsedSelect.Average());
        }

        private static async Task<List<long>> WorkloadTask(Func<Task> workload, Options options)
        {
            var elapsed = new List<long>();
            for (var i = 0; i < 5; i++)
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                await workload();
                stopWatch.Stop();
                elapsed.Add(stopWatch.ElapsedMilliseconds);
                if (options.Debug)
                {
                    Console.WriteLine("Throughput:\t{0:0} ops/s (elapsed {1:0})",
                        1000D * options.CqlRequests / stopWatch.ElapsedMilliseconds,
                        stopWatch.ElapsedMilliseconds);
                }
            }

            GC.Collect();
            return elapsed;
        }

        public class CountRetryPolicy : IRetryPolicy
        {
            private long _countRead;
            private long _countWrite;
            private long _countUnavailable;

            public long GetReadCount()
            {
                return Interlocked.Exchange(ref _countRead, 0L);
            }

            public long GetWriteCount()
            {
                return Interlocked.Exchange(ref _countWrite, 0L);
            }

            public long GetUnavailableCount()
            {
                return Interlocked.Exchange(ref _countUnavailable, 0L);
            }

            public RetryDecision OnReadTimeout(IStatement query, ConsistencyLevel cl, int requiredResponses, int receivedResponses,
                bool dataRetrieved, int nbRetry)
            {
                Interlocked.Increment(ref _countRead);
                return RetryDecision.Ignore();
            }

            public RetryDecision OnWriteTimeout(IStatement query, ConsistencyLevel cl, string writeType, int requiredAcks, int receivedAcks,
                int nbRetry)
            {
                Interlocked.Increment(ref _countWrite);
                return RetryDecision.Ignore();
            }

            public RetryDecision OnUnavailable(IStatement query, ConsistencyLevel cl, int requiredReplica, int aliveReplica, int nbRetry)
            {
                Interlocked.Increment(ref _countUnavailable);
                return RetryDecision.Ignore();
            }
        }
    }
}
