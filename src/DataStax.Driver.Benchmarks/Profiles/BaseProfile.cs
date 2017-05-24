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

        private Options _options;

        public virtual void Init(Options options)
        {
            _options = options;
            foreach (var q in InitQueries)
            {
                Task.Run(async () =>
                {
                    //await profile.Init(Options);
                    await ExecuteAsync(q);
                }).Wait();
            }
            PrepareStatements();
            // Warmup
            Task.Run(async () =>
            {
                //await profile.Init(Options);
                await InsertMultiple(200);
                await SelectMultiple(200);
            }).Wait();
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

        private async Task InsertMultiple(long repeatLength = 0L)
        {
            InsertTimer = new Timer();
            if (repeatLength == 0)
            {
                repeatLength = _options.CqlRequests;
            }
            await Utils.Times(repeatLength, _options.MaxOutstandingRequests, ExecuteInsertCqlAsync);
        }

        private async Task SelectMultiple(long repeatLength = 0L)
        {
            SelectTimer = new Timer();
            if (repeatLength == 0)
            {
                repeatLength = _options.CqlRequests;
            }
            await Utils.Times(repeatLength, _options.MaxOutstandingRequests, ExecuteSelectCqlAsync);
        }

        protected Task ExecuteInsertCqlAsync(long index)
        {
            return ExecuteInsertAsync(index);
            //return Task.Run(() =>
            //{
            //    var record = new Timer.TimeLogRecorder(InsertTimer);
            //    ExecuteInsertAsync(index);
            //    return record;
            //})
            //.ContinueWith((antecedent) =>
            //    {
            //        antecedent.Result.StopRecording();
            //    }, TaskContinuationOptions.ExecuteSynchronously);
        }

        protected Task ExecuteSelectCqlAsync(long index)
        {
            return ExecuteSelectAsync(index);
            //return Task.Run(() =>
            //{
            //    var record = new Timer.TimeLogRecorder(SelectTimer);
            //    ExecuteSelectAsync(index);
            //    return record;
            //})
            //.ContinueWith((antecedent) =>
            //{
            //    antecedent.Result.StopRecording();
            //}, TaskContinuationOptions.ExecuteSynchronously);
        }

        protected abstract Task ExecuteInsertAsync(long index);

        protected abstract Task ExecuteSelectAsync(long index);
    }
}
