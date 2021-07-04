using System.Threading.Tasks;

using Dse;

namespace DataStax.Driver.Benchmarks.Profiles
{
    internal class DseMinimalProfile : MinimalProfile
    {
        protected ISession Session;
        protected PreparedStatement InsertPs;
        protected PreparedStatement SelectPs;

        public DseMinimalProfile(ISession session)
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
                InsertPs = await Session.PrepareAsync(InsertQuery).ConfigureAwait(false);
            }
            if (SelectQuery != null)
            {
                SelectPs = await Session.PrepareAsync(SelectQuery).ConfigureAwait(false);
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