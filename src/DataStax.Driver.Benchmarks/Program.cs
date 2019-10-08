using System;
using CommandLine;

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
            testScript.Run(options).Wait();
        }

        private static ITestScript CreateTestScript(Options options)
        {
            switch (options.Driver)
            {
                case "dse":
                    return new DseTestScript();
                case "cassandra":
                case "cassandra-private":
                    return new CassandraTestScript();
                default:
                    throw new ArgumentException("driver parameter is invalid: " + options.Driver + ". Must be dse, cassandra or cassandra-private");
            }
        }
    }
}
