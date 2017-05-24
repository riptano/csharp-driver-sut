using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
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
        static void Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<Options>(args);
            var options = result.MapResult(o => o, e => null);
            if (options == null)
            {
                return;
            }
            var testScript = CreateTestScript(options);
            testScript.Run(options);
        }

        private static ITestScript CreateTestScript(Options options)
        {
            if (options.Driver == "dse")
            {
                return new DseTestScript();
            }
            return new CassandraTestScript();
        }
    }
}
