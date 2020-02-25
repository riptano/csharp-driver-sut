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
                await ExecuteAsync(q).ConfigureAwait(false);
            }

            await PrepareStatementsAsync().ConfigureAwait(false);

            // Warmup
            await InsertMultiple(Options.WarmupRequests).ConfigureAwait(false);
            await SelectMultiple(Options.WarmupRequests).ConfigureAwait(false);
        }
        protected abstract Task PrepareStatementsAsync();

        protected abstract Task ExecuteAsync(string query);
        protected abstract Task PrepareAsync(string query);

        public async Task<Timer> Insert()
        {
            await InsertMultiple().ConfigureAwait(false);
            return InsertTimer;
        }

        public async Task<Timer> Select()
        {
            await SelectMultiple().ConfigureAwait(false);
            return SelectTimer;
        }

        protected Task InsertMultiple(long repeatLength = 0L)
        {
            InsertTimer = new Timer();
            if (repeatLength == 0)
            {
                repeatLength = Options.CqlRequests;
            }
            return Utils.RunMultipleThreadsAsync(repeatLength, Options.CurrentOutstandingRequests, ExecuteInsertCqlAsync);
        }

        protected Task SelectMultiple(long repeatLength = 0L)
        {
            SelectTimer = new Timer();
            if (repeatLength == 0)
            {
                repeatLength = Options.CqlRequests;
            }
            return Utils.RunMultipleThreadsAsync(repeatLength, Options.CurrentOutstandingRequests, ExecuteSelectCqlAsync);
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
