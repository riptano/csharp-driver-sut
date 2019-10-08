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
            if (options.Driver == "dse")
            {
                return new DseTestScript();
            }
            return new CassandraTestScript();
        }
    }
}
