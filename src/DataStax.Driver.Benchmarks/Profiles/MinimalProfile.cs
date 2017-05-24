using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStax.Driver.Benchmarks.Profiles
{
    public abstract class MinimalProfile : BaseProfile
    {
        protected static readonly byte[] Value = new byte[] { 0x0F };
        protected static readonly DateTimeOffset Timestamp = DateTimeOffset.Now;

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
                var replicationFactor = GetReplicationFactor();
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

        protected abstract int GetReplicationFactor();
    }
}
