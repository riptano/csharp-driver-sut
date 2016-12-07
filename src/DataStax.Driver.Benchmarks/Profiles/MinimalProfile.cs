using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStax.Driver.Benchmarks.Profiles
{
    public class MinimalProfile : BaseProfile
    {
        private static readonly byte[] Value = new byte[] { 0x0F };
        private static readonly DateTimeOffset Timestamp = DateTimeOffset.Now;

        protected override string SelectQuery
        {
            get { return "SELECT key FROM test_csharp_benchmarks_minimal.minimal WHERE KEY = ?"; }
        }

        protected override string InsertQuery
        {
            get { return "INSERT INTO test_csharp_benchmarks_minimal.minimal (key) VALUES (?)"; }
        }

        protected override IEnumerable<string> InitQueries
        {
            get
            {
                var replicationFactor = Session.Cluster.AllHosts().Count > 3 ? 3 : Session.Cluster.AllHosts().Count;
                return new string[]
                {
                    "DROP KEYSPACE IF EXISTS test_csharp_benchmarks_minimal",
                    string.Format(
                        "CREATE KEYSPACE test_csharp_benchmarks_minimal " +
                        "WITH replication = {{'class': 'SimpleStrategy', 'replication_factor' : {0}}} and " +
                        "durable_writes = false", replicationFactor),
                    "CREATE TABLE test_csharp_benchmarks_minimal.minimal " +
                    "(key blob PRIMARY KEY)"
                };
            }
        }

        protected override Task ExecuteInsertAsync(long index)
        {
            return Session.ExecuteAsync(InsertPs.Bind(Value).SetTimestamp(Timestamp));
        }

        protected override Task ExecuteSelectAsync(long index)
        {
            return Session.ExecuteAsync(SelectPs.Bind(Value).SetTimestamp(Timestamp));
        }
    }
}
