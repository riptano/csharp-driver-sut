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
                    .SetCoreConnectionsPerHost(HostDistance.Local, 1)
                    .SetMaxConnectionsPerHost(HostDistance.Local, 1)
                    .SetMaxSimultaneousRequestsPerConnectionTreshold(HostDistance.Local, 2048))
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
            var repository = new Repository(session, metrics, options);
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
            //single instance of repository
            var repository = new Repository(session, new EmptyMetricsTracker(), options);
            repository.Execute<object>(repository.Preallocate<UserCredentials>(true, 100), false).Wait();
            repository.Execute<string>(repository.Preallocate<UserCredentials>(false, 100), false).Wait();
            const int statementLength = 40000;
            var statements = repository.Preallocate<UserCredentials>(true, statementLength);
            Task.Run(async () =>
            {
                var elapsed = new List<long>();
                for (var i = 0; i < 5; i++)
                {
                    // ReSharper disable once AccessToModifiedClosure
                    elapsed.Add(await repository.Execute<object>(statements, false));
                }
                var averageMs = elapsed.Average();
                Console.WriteLine("Insert Throughput:\n\tAverage {0:0} ops/s (elapsed {1:0}) - Median {2} ops/s", 
                    1000 * statementLength / averageMs, 
                    averageMs, 
                    1000 * statementLength / elapsed.OrderBy(x => x).Skip(2).First());
            }).Wait();
            statements = repository.Preallocate<UserCredentials>(false, statementLength);
            Task.Run(async () =>
            {
                var elapsed = new List<long>();
                for (var i = 0; i < 3; i++)
                {
                    elapsed.Add(await repository.Execute<string>(statements, false));
                }
                var averageMs = elapsed.Average();
                Console.WriteLine("Select Throughput:\n\tAverage {0:0} ops/s (elapsed {1:0}) - Min {2} ops/s",
                    1000 * statementLength / averageMs,
                    averageMs,
                    1000 * statementLength / elapsed.Max());
            }).Wait();
        }
    }
}
