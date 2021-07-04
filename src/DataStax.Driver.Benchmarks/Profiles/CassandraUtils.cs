using System.Threading.Tasks;

using Cassandra;

namespace DataStax.Driver.Benchmarks.Profiles
{
    internal class CassandraUtils
    {
        public static Task<PreparedStatement> PrepareStatement(ISession session, string query)
        {
            return session.PrepareAsync(query);
        }
    }
}