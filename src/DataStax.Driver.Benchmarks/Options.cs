﻿using System;
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
        [Option('p', HelpText = "Level of parallelism", Default = 64)]
        public int Parallelism { get; set; }
        [Option('o', HelpText = "Maximum outstanding requests per host", Default = 2048)]
        public int MaxOutstandingRequests { get; set; }
        [Option('r', HelpText = "Amount of driver requests per http request", Default = 1000)]
        public int CqlRequestsPerHttpRequest { get; set; }
        [Option('m', HelpText = "Metrics endpoint", Default = "127.0.0.1:2003")]
        public string MetricsEndpoint { get; set; }
    }
}