using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassandra;

namespace DataStax.Driver.Benchmarks.Profiles
{
    class CassandraMinimalProfile :  MinimalProfile
    {
        protected ISession Session;
        protected PreparedStatement InsertPs;
        protected PreparedStatement SelectPs;
        
        public CassandraMinimalProfile(ISession session)
        {
            this.Session = session;
        }

        protected override Task ExecuteInsertAsync(long index)
        {
            return Session.ExecuteAsync(InsertPs.Bind(Value).SetTimestamp(Timestamp));
        }

        protected override Task ExecuteSelectAsync(long index)
        {
            return Session.ExecuteAsync(SelectPs.Bind(Value).SetTimestamp(Timestamp));
        }

        protected override async Task PrepareStatementsAsync()
        {
            if (InsertQuery != null)
            {
                InsertPs = await CassandraUtils.PrepareStatement(Session, InsertQuery).ConfigureAwait(false);
            }
            if (SelectQuery != null)
            {
                SelectPs = await CassandraUtils.PrepareStatement(Session, SelectQuery).ConfigureAwait(false);
            }
        }

        protected override Task ExecuteAsync(string query)
        {
            return Session.ExecuteAsync(new SimpleStatement(query));
        }

        protected override Task PrepareAsync(string query)
        {
            return Session.PrepareAsync(query);
        }

        protected override int GetReplicationFactor()
        {
            return Session.Cluster.AllHosts().Count > 3 ? 3 : Session.Cluster.AllHosts().Count;
        }
    }
}
