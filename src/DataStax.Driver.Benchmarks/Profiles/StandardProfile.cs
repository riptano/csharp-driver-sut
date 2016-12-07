using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassandra;

namespace DataStax.Driver.Benchmarks.Profiles
{
    public class StandardProfile : BaseProfile
    {
        protected override string SelectQuery
        {
            get
            {
                return "SELECT key, c0, c1, c2, c3, c4 FROM test_csharp_benchmarks_standard.standard1" +
                       " WHERE key = ?";
            }
        }

        protected override string InsertQuery
        {
            get
            {
                return "INSERT INTO test_csharp_benchmarks_standard.standard1" +
                                             "(key, c0, c1, c2, c3, c4) VALUES (?, ?, ?, ?, ?, ?)";
            }
        }

        protected override IEnumerable<string> InitQueries
        {
            get
            {
                return new string[]
                {
                    "DROP KEYSPACE IF EXISTS test_csharp_benchmarks_standard",
                    string.Format(
                        "CREATE KEYSPACE test_csharp_benchmarks_standard " +
                        "WITH replication = {{'class': 'SimpleStrategy', 'replication_factor' : 1}} and " +
                        "durable_writes = true"),
                    "CREATE TABLE test_csharp_benchmarks_standard.standard1 " +
                    "(key blob PRIMARY KEY,c0 blob,c1 blob,c2 blob,c3 blob,c4 blob)"
                };
            }
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
