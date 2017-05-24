using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassandra;

namespace DataStax.Driver.Benchmarks.Profiles
{
    class CassandraUtils
    {
        public static PreparedStatement PrepareStatement(ISession session, string query)
        {
            PreparedStatement result = null;
            Task.Run(async () =>
            {
                result = await session.PrepareAsync(query);
            }).Wait();
            return result;
        }
    }
}
