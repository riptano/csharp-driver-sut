using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassandra;
using DataStax.Driver.Benchmarks.Metrics;

namespace DataStax.Driver.Benchmarks.Profiles
{
    public abstract class BaseProfile : IProfile
    {
        protected Timer InsertTimer = new Timer();
        protected Timer SelectTimer = new Timer();

        protected abstract string SelectQuery { get; }

        protected abstract string InsertQuery { get; }

        protected abstract IEnumerable<string> InitQueries { get; }

        protected Options Options;

        public virtual async Task Init(Options options)
        {
            Options = options;
            foreach (var q in InitQueries)
            {
                await ExecuteAsync(q);
            }
            PrepareStatements();
            // Warmup
            await InsertMultiple(20000);
            await SelectMultiple(20000);
        }
        protected abstract void PrepareStatements();

        protected abstract Task ExecuteAsync(string query);
        protected abstract Task PrepareAsync(string query);

        public async Task<Timer> Insert()
        {
            await InsertMultiple();
            return InsertTimer;
        }

        public async Task<Timer> Select()
        {
            await SelectMultiple();
            return SelectTimer;
        }

        protected async Task InsertMultiple(long repeatLength = 0L)
        {
            InsertTimer = new Timer();
            if (repeatLength == 0)
            {
                repeatLength = Options.CqlRequests;
            }
            await Utils.Times(repeatLength, Options.MaxOutstandingRequests, ExecuteInsertCqlAsync);
        }

        protected async Task SelectMultiple(long repeatLength = 0L)
        {
            SelectTimer = new Timer();
            if (repeatLength == 0)
            {
                repeatLength = Options.CqlRequests;
            }
            await Utils.Times(repeatLength, Options.MaxOutstandingRequests, ExecuteSelectCqlAsync);
        }

        protected Task ExecuteInsertCqlAsync(long index)
        {
            return ExecuteInsertAsync(index);
        }

        protected Task ExecuteSelectCqlAsync(long index)
        {
            return ExecuteSelectAsync(index);
        }

        protected abstract Task ExecuteInsertAsync(long index);

        protected abstract Task ExecuteSelectAsync(long index);
    }
}
