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
using Microsoft.Owin.Hosting;

namespace DataStax.Driver.Benchmarks
{
    public class Program
    {
        static void Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<Options>(args);
            var options = result.MapResult(o => o, e => null);
            if (options == null)
            {
                return;
            }
            Diagnostics.CassandraTraceSwitch.Level = TraceLevel.Info;
            Trace.Listeners.Add(new ConsoleTraceListener());
            var driverVersion = Version.Parse(FileVersionInfo.GetVersionInfo(
                Assembly.GetAssembly(typeof (ISession)).Location).FileVersion);
            Console.WriteLine("Using driver version {0}", driverVersion);
            var cluster = Cluster.Builder()
                .AddContactPoint(options.ContactPoint)
                .WithSocketOptions(new SocketOptions().SetTcpNoDelay(true).SetReadTimeoutMillis(0))
                .WithLoadBalancingPolicy(new RoundRobinPolicy())
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
                Console.WriteLine("Finished, press any key to continue...");
                Console.Read();
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
            var metrics = new EmptyMetricsTracker();
            //single instance of repository
            var repository = new Repository(session, metrics, false, options);
            var statementLength = options.CqlRequests;
            Task.Run(async () =>
            {
                var elapsed = new List<long>();
                for (var i = 0; i < 5; i++)
                {
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    await repository.Insert(new UserCredentials {Email = "a", Password = "b", UserId = Guid.NewGuid()});
                    stopWatch.Stop();
                    elapsed.Add(stopWatch.ElapsedMilliseconds);
                }
                var averageMs = elapsed.Average();
                Console.WriteLine("Insert Throughput:\n\tAverage {0:0} ops/s (elapsed {1:0}) - Median {2} ops/s", 
                    1000D * statementLength / averageMs, 
                    averageMs, 
                    1000D * statementLength / elapsed.OrderBy(x => x).Skip(2).First());
            }).Wait();
            GC.Collect();
            Task.Run(async () =>
            {
                var elapsed = new List<long>();
                for (var i = 0; i < 5; i++)
                {
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    await repository.GetCredentials("a");
                    stopWatch.Stop();
                    elapsed.Add(stopWatch.ElapsedMilliseconds);
                }
                var averageMs = elapsed.Average();
                Console.WriteLine("Select Throughput:\n\tAverage {0:0} ops/s (elapsed {1:0}) - Median {2} ops/s",
                    1000D * statementLength / averageMs,
                    averageMs,
                    1000D * statementLength / elapsed.OrderBy(x => x).Skip(2).First());
            }).Wait();
        }
    }
}
