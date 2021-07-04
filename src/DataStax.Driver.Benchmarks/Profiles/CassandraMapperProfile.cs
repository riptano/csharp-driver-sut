using System;
using System.Threading.Tasks;
using Cassandra;
using Cassandra.Mapping;

namespace DataStax.Driver.Benchmarks.Profiles
{
    class CassandraMapperProfile : MapperStandardProfile
    {
        private IMapper _mapper;
        protected ISession Session;
        protected PreparedStatement InsertPs;
        protected PreparedStatement SelectPs;

        public CassandraMapperProfile(ISession session)
        {
            this.Session = session;
        }
        public override async Task Init(Options options)
        {
            MappingConfiguration.Global.Define(new Map<StandardPoco>()
                .PartitionKey(p => p.Key)
                .KeyspaceName("test_csharp_benchmarks_standard_mapper")
                .TableName("standard1"));

            _mapper = new Mapper(Session);
            await base.Init(options).ConfigureAwait(false);
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
        protected override Task ExecuteSelectAsync(long index)
        {
            var value = BitConverter.GetBytes(index);
            return _mapper.FetchAsync<StandardPoco>(SelectQuery, value);
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
    }
}
