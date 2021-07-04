using System;
using System.Threading.Tasks;

using Cassandra;

namespace DataStax.Driver.Benchmarks.Profiles
{
    internal class CassandraStandardProfile : StandardProfile
    {
        protected ISession Session;
        protected PreparedStatement InsertPs;
        protected PreparedStatement SelectPs;

        public CassandraStandardProfile(ISession session)
        {
            Session = session;
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

        protected override Task ExecuteInsertAsync(long index)
        {
            var val = BitConverter.GetBytes(index);
            return Session.ExecuteAsync(InsertPs.Bind(val, val, val, val, val, val));
        }

        protected override Task ExecuteSelectAsync(long index)
        {
            var val = BitConverter.GetBytes(index);
            return Session.ExecuteAsync(SelectPs.Bind(val));
        }
    }
}