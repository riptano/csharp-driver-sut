using System;
using System.Diagnostics;
using System.Reflection;
using DataStax.Driver.Benchmarks.Profiles;
using Dse;

namespace DataStax.Driver.Benchmarks
{
    class DseTestScript : BaseTestScript
    {
        private IDseCluster _cluster;
        private IDseSession _session;
        protected override void Setup()
        {
            Diagnostics.CassandraTraceSwitch.Level = Options.Debug ? TraceLevel.Info : TraceLevel.Warning;
            Trace.Listeners.Add(new ConsoleTraceListener());
            DriverVersion = Options.Version;
            _cluster = DseCluster.Builder()
                .AddContactPoint(Options.ContactPoint)
                .WithSocketOptions(new SocketOptions().SetTcpNoDelay(true).SetReadTimeoutMillis(0))
                .WithRetryPolicy(RetryPolicy)
                .WithPoolingOptions(new PoolingOptions()
                    .SetCoreConnectionsPerHost(HostDistance.Local, Options.ConnectionsPerHost)
                    .SetMaxConnectionsPerHost(HostDistance.Local, Options.ConnectionsPerHost)
                    .SetMaxSimultaneousRequestsPerConnectionTreshold(HostDistance.Local, 2048))
                .WithQueryOptions(new QueryOptions().SetConsistencyLevel(ConsistencyLevel.LocalOne))
                .Build();
            _session = _cluster.Connect();
        }

        protected override void Shutdown()
        {
            _cluster.Shutdown(3000);
        }
        protected override IProfile GetProfile()
        {
            // In the future we can create the type instance by name
            // for now, its good enough
            switch (Options.Profile)
            {
                case "minimal":
                    return new DseMinimalProfile(_session);
                case "mapper":
                    return new DseMapperProfile(_session);
                default:
                    return new DseStandardProfile(_session);
            }
        }

    }
}
