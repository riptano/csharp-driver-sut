using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassandra;

namespace DataStax.Driver.Benchmarks.Profiles
{
    public abstract class BaseProfile : IProfile
    {
        protected abstract string SelectQuery { get; }

        protected abstract string InsertQuery { get; }

        protected abstract IEnumerable<string> InitQueries { get; }

        protected ISession Session;
        private Options _options;
        protected PreparedStatement InsertPs;
        protected PreparedStatement SelectPs;

        public virtual async Task Init(ISession session, Options options)
        {
            Session = session;
            _options = options;
            foreach (var q in InitQueries)
            {
                await session.ExecuteAsync(new SimpleStatement(q));
            }
            if (InsertQuery != null)
            {
                InsertPs = await session.PrepareAsync(InsertQuery);
            }
            if (SelectQuery != null)
            {
                SelectPs = await session.PrepareAsync(SelectQuery);
            }
            // Warmup
            await InsertMultiple(20000);
            await SelectMultiple(20000);
        }

        public async Task Insert()
        {
            await InsertMultiple();
        }

        public async Task Select()
        {
            await SelectMultiple();
        }

        private async Task InsertMultiple(long repeatLength = 0L)
        {
            if (repeatLength == 0)
            {
                repeatLength = _options.CqlRequests;
            }
            await Utils.Times(repeatLength, _options.MaxOutstandingRequests, ExecuteInsertAsync);
        }

        private async Task SelectMultiple(long repeatLength = 0L)
        {
            if (repeatLength == 0)
            {
                repeatLength = _options.CqlRequests;
            }
            await Utils.Times(repeatLength, _options.MaxOutstandingRequests, ExecuteSelectAsync);
        }

        protected abstract Task ExecuteInsertAsync(long index);

        protected abstract Task ExecuteSelectAsync(long index);
    }
}
