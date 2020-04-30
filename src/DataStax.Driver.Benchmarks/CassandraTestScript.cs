using System;
using System.Diagnostics;
#if !NET452 && !NETCOREAPP2_0
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Reporting.Graphite;
using App.Metrics.Scheduling;
#endif
using Cassandra;
using Cassandra.Metrics;
using DataStax.Driver.Benchmarks.Profiles;

namespace DataStax.Driver.Benchmarks
{
    class CassandraTestScript : BaseTestScript
    {
        private ICluster _cluster;
        private ISession _session;
        protected override void Setup()
        {
            Diagnostics.CassandraTraceSwitch.Level = Options.Debug ? TraceLevel.Info : TraceLevel.Warning;
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
            DriverVersion = Options.Version;
            var builder = Cluster.Builder()
                .AddContactPoint(Options.ContactPoint)
                .WithSocketOptions(new SocketOptions().SetTcpNoDelay(true).SetReadTimeoutMillis(Options.ReadTimeoutMillis).SetStreamMode(Options.StreamMode))
                .WithRetryPolicy(RetryPolicy)
                .WithPoolingOptions(new PoolingOptions()
                    .SetCoreConnectionsPerHost(HostDistance.Local, Options.ConnectionsPerHost)
                    .SetMaxConnectionsPerHost(HostDistance.Local, Options.ConnectionsPerHost)
                    .SetMaxSimultaneousRequestsPerConnectionTreshold(HostDistance.Local, 2048))
                .WithQueryOptions(new QueryOptions().SetConsistencyLevel(ConsistencyLevel.LocalOne));

            if (Options.Compression != null)
            {
                if (Options.Compression.Equals("lz4", StringComparison.OrdinalIgnoreCase))
                {
                    builder = builder.WithCompression(CompressionType.LZ4);
                }
                else if (Options.Compression.Equals("snappy", StringComparison.OrdinalIgnoreCase))
                {
                    builder = builder.WithCompression(CompressionType.Snappy);
                }
                else
                {
                    throw new ArgumentException("Unknown compression type");
                }
            }

            builder = ConfigureAppMetrics(builder);
            
            _cluster = builder.Build();
            _session = _cluster.Connect();
        }

        protected override void Shutdown()
        {
            _cluster.Shutdown();
        }

        protected override IProfile GetProfile()
        {
            // In the future we can create the type instance by name
            // for now, its good enough
            switch (Options.Profile)
            {
                case "minimal":
                    return new CassandraMinimalProfile(_session);
                case "mapper":
                    return new CassandraMapperProfile(_session);
                default:
                    return new CassandraStandardProfile(_session);
            }
        }

        private Builder ConfigureAppMetrics(Builder builder)
        {
            if (Options.AppMetrics)
            {
#if NET452 || NETCOREAPP2_0
                throw new ArgumentException("App Metrics are not supported in NET452 and NETSTANDARD1.5");
#else
                var metricsRoot = new MetricsBuilder()
                    .Report.ToGraphite(opt =>
                    {
                        opt.Graphite = new GraphiteOptions(new Uri(Options.MetricsEndpoint));
                        opt.FlushInterval = TimeSpan.FromMilliseconds(Options.AppMetricsInterval);
                    })
                    .Build();
                
                var scheduler = new AppMetricsTaskScheduler(
                    TimeSpan.FromMilliseconds(Options.AppMetricsInterval),
                    async () => { await Task.WhenAll(metricsRoot.ReportRunner.RunAllAsync()); });

                scheduler.Start();

                var metricOptions = new DriverMetricsOptions().SetBucketPrefix(
                    $"{Options.Driver.Replace('.', '_')}" +
                    $".{Options.Version.Replace('.', '_')}" +
                    $".{Options.Framework.Replace('.', '_')}" +
                    $".{Options.Profile.Replace('.', '_')}");

                if (Options.TimerMetrics)
                {
                    metricOptions = metricOptions
                        .SetEnabledNodeMetrics(NodeMetric.AllNodeMetrics)
                        .SetEnabledSessionMetrics(SessionMetric.AllSessionMetrics);
                }

                return builder.WithMetrics(
                    metricsRoot.CreateDriverMetricsProvider(), 
                    metricOptions);
#endif
            }

            return builder;
        }
    }
}
