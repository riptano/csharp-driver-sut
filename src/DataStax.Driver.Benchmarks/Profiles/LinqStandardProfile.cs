using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Cassandra;
using Cassandra.Data.Linq;
using Cassandra.Mapping;
using DataStax.Driver.Benchmarks.Models;

namespace DataStax.Driver.Benchmarks.Profiles
{
    public class LinqStandardProfile : BaseProfile
    {
        private Table<StandardPoco> _table;

        protected override string SelectQuery { get { return null; } }

        protected override string InsertQuery { get { return null; } }

        protected override IEnumerable<string> InitQueries
        {
            get
            {
                return new string[]
                {
                    "DROP KEYSPACE IF EXISTS test_csharp_benchmarks_standard_linq",
                    string.Format(
                        "CREATE KEYSPACE test_csharp_benchmarks_standard_linq " +
                        "WITH replication = {{'class': 'SimpleStrategy', 'replication_factor' : 1}} and " +
                        "durable_writes = false"),
                    "USE test_csharp_benchmarks_standard_linq",
                    "CREATE TABLE standard1 " +
                    "(key blob PRIMARY KEY,c0 blob,c1 blob,c2 blob,c3 blob,c4 blob)"
                };
            }
        }

        public override Task Init(ISession session, Options options)
        {
            var config = new MappingConfiguration().Define(new Map<StandardPoco>()
                .PartitionKey(p => p.Key)
                .TableName("standard1"));
            _table = new Table<StandardPoco>(session, config);
            return base.Init(session, options);
        }

        protected override Task ExecuteInsertAsync(long index)
        {
            var value = BitConverter.GetBytes(index);
            return _table.Insert(new StandardPoco
            {
                Key = value,
                C0 = value,
                C1 = value,
                C2 = value,
                C3 = value,
                C4 = value
            }).ExecuteAsync();
        }

        protected override Task ExecuteSelectAsync(long index)
        {
            var value = BitConverter.GetBytes(index);
            return _table.Where(t => t.Key == value).Select(t => new StandardPoco
            {
                C0 = t.C0,
                C1 = t.C1,
                C2 = t.C2
            }).ExecuteAsync();
        }
    }
}
