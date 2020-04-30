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
        [Option('c', HelpText = "The Cluster contact point")]
        public string ContactPoint { get; set; }
        
        [Option('p', HelpText = "Amount of connections per host", Default = 1)]
        public int ConnectionsPerHost { get; set; }
        
        [Option('e', HelpText = "Number of requests for warmup", Default = 100000)]
        public int WarmupRequests { get; set; }

        [Option('o', HelpText = "Maximum outstanding requests per host", Default = "128,256,512,1024")]
        public string MaxOutstandingRequestsStr { get; set; }

        internal int CurrentOutstandingRequests { get; set; }

        [Option('r', HelpText = "Amount of CQL requests per call (http request or script)", Default = 10000)]
        public int CqlRequests { get; set; }

        [Option('h', HelpText = "Socket Read Timeout Milliseconds", Default = 0)]
        public int ReadTimeoutMillis { get; set; }

        [Option('s', HelpText = "Amount of series", Default = 5)]
        public int Series { get; set; }

        [Option('m', HelpText = "Metrics endpoint", Default = null)]
        public string MetricsEndpoint { get; set; }

        [Option('d', HelpText = "Debug", Default = false)]
        public bool Debug { get; set; }

        [Option('w', HelpText = "The workload profile (standard|minimal|mapper)", Default = "standard")]
        public string Profile { get; set; }
        
        [Option('a', HelpText = "Enable App Metrics", Default = false)]
        public bool AppMetrics { get; set; }

        [Option('i', HelpText = "App Metrics report interval (miliseconds)", Default = 10000)]
        public int AppMetricsInterval { get; set; }

        [Option('t', HelpText = "Enable Timer Metrics", Default = false)]
        public bool TimerMetrics { get; set; }
        
        [Option('b', HelpText = "Enable Socket Stream Mode", Default = false)]
        public bool StreamMode { get; set; }
        
        [Option("compression", HelpText = "Compression (lz4|snappy)", Default = null)]
        public string Compression { get; set; }
        
        [Value(2, HelpText = "Target Framework", Required = true)]
        public string Framework { get; set; }

        [Value(0, Required = true)]
        public string Driver { get; set; }

        private string _version;

        [Value(1, Required = true)]
        public string Version {
            get
            {
                if (_version.Contains("/"))
                {
                    return _version.Split('/')[1];
                }
                return _version;
            }
            set { _version = value; }
        }
    }
}
