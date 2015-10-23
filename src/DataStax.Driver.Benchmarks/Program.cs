using Microsoft.Owin.Hosting;
using Owin;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using Cassandra;
using System.Web.Http.Routing;
using CommandLine;
using DataStax.Driver.Benchmarks.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DataStax.Driver.Benchmarks
{
    public class Program
    {
        public class CommandLineArguments
        {
            [Option('c', HelpText = "The Cluster contact point", Default = "127.0.0.1")]
            public string ContactPoint { get; set; }
            [Option('u', HelpText = "The web app entry point", Default = "http://localhost:8081/")]
            public string Url { get; set; }
            [Option('h', HelpText = "Specifies that http server should be created (Y/N)", Default = 'Y')]
            public char UseHttp { get; set; }
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
            Console.WriteLine(options.UseHttp);
            Console.WriteLine(options.Url);
            Console.WriteLine(options.ContactPoint);
            Diagnostics.CassandraTraceSwitch.Level = TraceLevel.Info;
            Trace.Listeners.Add(new ConsoleTraceListener());
            var cluster = Cluster.Builder()
                .AddContactPoint(options.ContactPoint)
                .WithSocketOptions(new SocketOptions().SetTcpNoDelay(false))
                .WithPoolingOptions(new PoolingOptions()
                    .SetCoreConnectionsPerHost(HostDistance.Local, 4)
                    .SetMaxConnectionsPerHost(HostDistance.Local, 4)
                    .SetMaxSimultaneousRequestsPerConnectionTreshold(HostDistance.Local, 2048))
                .Build();
            Session = cluster.Connect();
            if (options.UseHttp.ToString().ToUpperInvariant() != "Y")
            {
                SingleScript(Session);
                Console.WriteLine("Finished, press any key to continue...");
                Console.Read();
                return;
            }
            Console.WriteLine("Starting web server");
            using (WebApp.Start<Startup>(options.Url))
            {
                Console.WriteLine("Server running on " + options.Url);
                Console.ReadLine();
            }
            cluster.Shutdown(3000);
        }

        private static void SingleScript(ISession session)
        {
            //single instance of repository
            var repository = new Repository(Session);
            var statements = repository.Preallocate<UserCredentials>(10);
            repository.Execute(statements).Wait();
            const int statementLength = 40000;
            statements = repository.Preallocate<UserCredentials>(statementLength);
            Task.Run(async () =>
            {
                for (var i = 0; i < 5; i++)
                {
                    var ms = await repository.Execute(statements);
                    Console.WriteLine("Throughput: {0} ops/s (elapsed {1})", 1000 * statementLength / ms, ms);
                }
            }).Wait();
        }

        public class Startup
        {
            public void Configuration(IAppBuilder appBuilder)
            {
                var config = new HttpConfiguration();
                config.Routes.MapHttpRoute("Home", "", new { controller = "Main", action = "Index" });
                config.Routes.MapHttpRoute("Now", "cassandra", new { controller = "Main", action = "Now" });
                config.Routes.MapHttpRoute("JsonTest", "json-test", new
                {
                    controller = "Main", action = "JsonTest"
                }, new
                {
                    httpMethod = new HttpMethodConstraint(HttpMethod.Post)
                });
                config.Routes.MapHttpRoute("Insert-Prepared", "prepared-statements/users", new
                {
                    controller = "Main", action = "InsertCredentials", email = "fixed", password = "fixed", prepared = true
                }, new
                {
                    httpMethod = new HttpMethodConstraint(HttpMethod.Post)
                });
                //single instance of Repository
                //poor man's DI
                var repository = new Repository(Session);
                config.Services.Replace(typeof(IHttpControllerActivator), new SingleControllerActivator(repository));
                var jsonSettings = config.Formatters.JsonFormatter.SerializerSettings;
                jsonSettings.Formatting = Formatting.Indented;
                //Use camel case json
                jsonSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                appBuilder.UseWebApi(config);
            }
        }

        public class SingleControllerActivator : IHttpControllerActivator
        {
            private readonly Repository _repository;

            public SingleControllerActivator(Repository repository)
            {
                _repository = repository;
            }

            public IHttpController Create(HttpRequestMessage request, HttpControllerDescriptor controllerDescriptor, Type controllerType)
            {
                return new MainController(_repository);
            }
        } 
    }
}
