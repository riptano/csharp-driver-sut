using Microsoft.Owin.Hosting;
using Owin;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Web.Http;
using Cassandra;
using System.Web.Http.Routing;

namespace DataStax.Driver.Benchmarks
{
    public class Program
    {
        public static ISession Session;
        public static PreparedStatement InsertPs;
        public static PreparedStatement SelectPs;
        public const string InsertQuery = "INSERT INTO videodb.users (username, firstname, lastname, password, email, created_date) VALUES (?, ?, ?, ?, ?, ?)";
        public const string SelectQuery = "SELECT username FROM videodb.users WHERE username = ?";

        static void Main(string[] args)
        {
            var baseAddress = "http://localhost:8081/";
            var contactPoint = "127.0.0.1";
            var driverVersion = "master";

            if (args.Length > 0)
            {
                driverVersion = args[0];
            }
            if (args.Length > 1)
            {
                contactPoint = args[1];
            }
            if (args.Length > 2)
            {
                baseAddress = args[2];
            }
            Console.WriteLine("Starting web server with driver version " + driverVersion);
            Cassandra.Diagnostics.CassandraTraceSwitch.Level = TraceLevel.Info;
            Trace.Listeners.Add(new ConsoleTraceListener());
            var cluster = Cluster.Builder().AddContactPoint(contactPoint).Build();
            Session = cluster.Connect();
            InsertPs = Session.Prepare(InsertQuery);
            SelectPs = Session.Prepare(SelectQuery);
            using (WebApp.Start<Startup>(url: baseAddress))
            {
                Console.WriteLine("Server running on " + baseAddress);
                Console.ReadLine();
            }
            cluster.Shutdown(3000);
        }


        public class Startup
        {
            public void Configuration(IAppBuilder appBuilder)
            {
                var config = new HttpConfiguration();
                config.Routes.MapHttpRoute("Home", "", new { controller = "Main", action = "Index" });
                config.Routes.MapHttpRoute("Now", "cassandra", new { controller = "Main", action = "Now" });
                config.Routes.MapHttpRoute("Insert-Simple", "simple-statements/users/{start}/{length}", new
                {
                    controller = "Main", action = "Insert", length = 1, prepared = false
                }, new { httpMethod = new HttpMethodConstraint(HttpMethod.Post) });
                config.Routes.MapHttpRoute("Insert-Prepared", "prepared-statements/users/{start}/{length}", new
                {
                    controller = "Main", action = "Insert", length = 1, prepared = true
                }, new { httpMethod = new HttpMethodConstraint(HttpMethod.Post) });
                config.Routes.MapHttpRoute("Select-Simple", "simple-statements/users/{start}/{length}", new
                {
                    controller = "Main", action = "Select", length = 1, prepared = false
                }, new { httpMethod = new HttpMethodConstraint(HttpMethod.Get) });
                config.Routes.MapHttpRoute("Select-Prepared", "prepared-statements/users/{start}/{length}", new
                {
                    controller = "Main", action = "Select", length = 1, prepared = true
                }, new { httpMethod = new HttpMethodConstraint(HttpMethod.Get) });
                appBuilder.UseWebApi(config);
            }
        }
    }
}
