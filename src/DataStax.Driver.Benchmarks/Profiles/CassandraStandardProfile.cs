using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassandra;

namespace DataStax.Driver.Benchmarks.Profiles
{
    class CassandraStandardProfile : StandardProfile
    {
        protected ISession Session;
        protected PreparedStatement InsertPs;
        protected PreparedStatement SelectPs;

        public CassandraStandardProfile(ISession session)
        {
            Session = session;
        }

        protected override void PrepareStatements()
        {
            if (InsertQuery != null)
            {
                InsertPs = CassandraUtils.PrepareStatement(Session, InsertQuery);
            }
            if (SelectQuery != null)
            {
                SelectPs = CassandraUtils.PrepareStatement(Session, SelectQuery);
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
