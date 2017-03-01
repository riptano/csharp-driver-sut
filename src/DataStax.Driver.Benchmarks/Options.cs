using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace DataStax.Driver.Benchmarks
{
    public class Options
    {
        [Option('c', HelpText = "The Cluster contact point", Default = "127.0.0.1")]
        public string ContactPoint { get; set; }

        [Option('e', HelpText = "The web app entry point", Default = "http://localhost:8080/")]
        public string Url { get; set; }

        [Option('s', HelpText = "Specifies that http server should be created (Y/N)", Default = 'Y')]
        public char UseHttp { get; set; }

        [Option('p', HelpText = "Amount of connections per host", Default = 1)]
        public int ConnectionsPerHost { get; set; }

        [Option('o', HelpText = "Maximum outstanding requests per host", Default = 1024)]
        public int MaxOutstandingRequests { get; set; }

        [Option('r', HelpText = "Amount of CQL requests per call (http request or script)", Default = 10000)]
        public int CqlRequests { get; set; }

        [Option('m', HelpText = "Metrics endpoint", Default = "127.0.0.1:2003")]
        public string MetricsEndpoint { get; set; }

        [Option('d', HelpText = "Debug", Default = false)]
        public bool Debug { get; set; }

        [Option('w', HelpText = "The workload profile (standard|minimal|mapper|linq)", Default = "standard")]
        public string Profile { get; set; }
    }
}
