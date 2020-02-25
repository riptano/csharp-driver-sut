using System;
using System.Diagnostics;
using Cassandra;
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
                .WithSocketOptions(new SocketOptions().SetTcpNoDelay(true).SetReadTimeoutMillis(0).SetStreamMode(Options.StreamMode))
                .WithRetryPolicy(RetryPolicy)
                .WithPoolingOptions(new PoolingOptions()
                    .SetCoreConnectionsPerHost(HostDistance.Local, Options.ConnectionsPerHost)
                    .SetMaxConnectionsPerHost(HostDistance.Local, Options.ConnectionsPerHost)
                    .SetMaxSimultaneousRequestsPerConnectionTreshold(HostDistance.Local, 2048))
                .WithQueryOptions(new QueryOptions().SetConsistencyLevel(ConsistencyLevel.LocalOne));

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
            return builder;
        }
    }
}
