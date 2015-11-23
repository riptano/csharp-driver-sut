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
        public class CommandLineArguments
        {
            [Option('c', HelpText = "The Cluster contact point", Default = "127.0.0.1")]
            public string ContactPoint { get; set; }
            [Option('e', HelpText = "The web app entry point", Default = "http://localhost:8080/")]
            public string Url { get; set; }
            [Option('u', HelpText = "Specifies that http server should be created (Y/N)", Default = 'Y')]
            public char UseHttp { get; set; }
            [Option('o', HelpText = "Maximum outstanding requests", Default = 2048)]
            public int MaxOutstandingRequests { get; set; }
            [Option('p', HelpText = "Level of parallelism", Default = 64)]
            public int Parallelism { get; set; }
            [Option('m', HelpText = "Metrics endpoint", Default = "127.0.0.1:2003")]
            public string MetricsEndpoint { get; set; }
        }

        public static ISession Session;

        static void Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<CommandLineArguments>(args);
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
                .WithSocketOptions(new SocketOptions().SetTcpNoDelay(true))
                .WithLoadBalancingPolicy(new RoundRobinPolicy())
                .WithPoolingOptions(new PoolingOptions()
                    .SetCoreConnectionsPerHost(HostDistance.Local, 1)
                    .SetMaxConnectionsPerHost(HostDistance.Local, 1)
                    .SetMaxSimultaneousRequestsPerConnectionTreshold(HostDistance.Local, 2048))
                .Build();
            Session = cluster.Connect();
            if (options.UseHttp.ToString().ToUpperInvariant() != "Y")
            {
                Console.WriteLine("Using single script");
                SingleScript(Session, options.Parallelism, options.MaxOutstandingRequests);
                Console.WriteLine("Finished, press any key to continue...");
                Console.Read();
                return;
            }
            Console.WriteLine("Starting benchmarks web server...");
            var metricsHost = options.MetricsEndpoint.Split(':');
            var metrics = new MetricsTracker(new IPEndPoint(IPAddress.Parse(metricsHost[0]), Convert.ToInt32(metricsHost[1])), driverVersion);
            var repository = new Repository(Session, metrics, new SemaphoreSlim(options.MaxOutstandingRequests), options.Parallelism);
            using (WebApp.Start(options.Url, b => WebStartup.Build(repository, b)))
            {
                Console.WriteLine("Server running on " + options.Url);
                Console.ReadLine();
            }
            metrics.Dispose();
            cluster.Shutdown(3000);
        }

        private static void SingleScript(ISession session, int parallelism, int maxOutstandingRequests)
        {
            //single instance of repository
            var repository = new Repository(session, new EmptyMetricsTracker(), new SemaphoreSlim(maxOutstandingRequests), parallelism);
            repository.Execute<object>(repository.Preallocate<UserCredentials>(true, 100), false).Wait();
            repository.Execute<string>(repository.Preallocate<UserCredentials>(false, 100), false).Wait();
            const int statementLength = 50000;
            var statements = repository.Preallocate<UserCredentials>(true, statementLength);
            Task.Run(async () =>
            {
                var elapsed = new List<long>();
                for (var i = 0; i < 3; i++)
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
