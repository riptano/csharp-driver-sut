using System;
using System.Threading.Tasks;
using Dse;
using Dse.Mapping;

namespace DataStax.Driver.Benchmarks.Profiles
{
    class DseMapperProfile : MapperStandardProfile
    {
        private IMapper _mapper;
        protected ISession Session;
        protected PreparedStatement InsertPs;
        protected PreparedStatement SelectPs;

        public DseMapperProfile(ISession session)
        {
            this.Session = session;
        }
        public override void Init(Options options)
        {
            MappingConfiguration.Global.Define(new Map<StandardPoco>()
                .PartitionKey(p => p.Key)
                .KeyspaceName("test_csharp_benchmarks_standard_mapper")
                .TableName("standard1"));
            _mapper = new Mapper(Session);
        }

        protected override void PrepareStatements()
        {
            if (InsertQuery != null)
            {
                Task.Run(async () =>
                {
                    InsertPs = await Session.PrepareAsync(InsertQuery);
                }).Wait();
            }
            if (SelectQuery != null)
            {
                Task.Run(async () =>
                {
                    SelectPs = await Session.PrepareAsync(SelectQuery);
                }).Wait();
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
