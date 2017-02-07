using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassandra;
using Cassandra.Mapping;

namespace DataStax.Driver.Benchmarks.Profiles
{
    public class MapperStandardProfile : BaseProfile
    {
        private IMapper _mapper;

        protected override string SelectQuery
        {
            get
            {
                return "SELECT key, c0, c1, c2, c3, c4 FROM test_csharp_benchmarks_standard.standard1" +
                       " WHERE key = ?";
            }
        }

        protected override string InsertQuery { get { return null; } }
        
        protected override IEnumerable<string> InitQueries
        {
            get
            {
                return new string[]
                {
                    "DROP KEYSPACE IF EXISTS test_csharp_benchmarks_standard_mapper",
                    string.Format(
                        "CREATE KEYSPACE test_csharp_benchmarks_standard_mapper " +
                        "WITH replication = {{'class': 'SimpleStrategy', 'replication_factor' : 1}} and " +
                        "durable_writes = false"),
                    "CREATE TABLE test_csharp_benchmarks_standard_mapper.standard1 " +
                    "(key blob PRIMARY KEY,c0 blob,c1 blob,c2 blob,c3 blob,c4 blob)"
                };
            }
        }

        public override Task Init(ISession session, Options options)
        {
            MappingConfiguration.Global.Define(new Map<StandardPoco>()
                .PartitionKey(p => p.Key)
                .KeyspaceName("test_csharp_benchmarks_standard_mapper")
                .TableName("standard1"));
            _mapper = new Mapper(session);
            return base.Init(session, options);
        }

        protected override Task ExecuteInsertAsync(long index)
        {
            var value = BitConverter.GetBytes(index);
            return _mapper.InsertAsync(new StandardPoco
            {
                Key = value,
                C0 = value,
                C1 = value,
                C2 = value,
                C3 = value,
                C4 = value
            });
        }

        protected override Task ExecuteSelectAsync(long index)
        {
            var value = BitConverter.GetBytes(index);
            return _mapper.FetchAsync<StandardPoco>(SelectQuery, value);
        }

        private class StandardPoco
        {
            public byte[] Key { get; set; }

            public byte[] C0 { get; set; }

            public byte[] C1 { get; set; }

            public byte[] C2 { get; set; }

            public byte[] C3 { get; set; }

            public byte[] C4 { get; set; }
        }
    }
}
