using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cassandra;
using DataStax.Driver.Benchmarks.Models;

namespace DataStax.Driver.Benchmarks
{
    public class Repository
    {
        private static class Queries
        {
            public const string InsertCredentials = "INSERT INTO killrvideo.user_credentials (email, password, userid) VALUES (?, ?, ?)";
            public const string SelectCredentials = "SELECT userid FROM killrvideo.user_credentials WHERE email = ?";
        }

        private readonly ISession _session;
        private readonly IMetricsTracker _metrics;
        private readonly SemaphoreSlim _semaphore;
        private readonly int _repeatLength;
        private readonly PreparedStatement _insertPs;
        private readonly PreparedStatement _selectPs;

        public Repository(ISession session, IMetricsTracker metrics, Options options)
        {
            _session = session;
            _metrics = metrics;
            _semaphore = new SemaphoreSlim(options.MaxOutstandingRequests * session.Cluster.AllHosts().Count);
            _repeatLength = options.CqlRequests;
            _insertPs = session.Prepare(Queries.InsertCredentials);
            _selectPs = session.Prepare(Queries.SelectCredentials);
        }

        public async Task Insert(UserCredentials credentials)
        {
            credentials.UserId = Guid.NewGuid();
            await ExecuteMultiple("prepared-insert-user_credentials", _insertPs, credentials.Email, credentials.Password, credentials.UserId);
        }

        internal async Task<UserCredentials> GetCredentials(string email)
        {
            var rs = await ExecuteMultiple("prepared-select-user_credentials", _selectPs, email);
            var row = rs.FirstOrDefault();
            if (row == null)
            {
                return null;
            }
            return new UserCredentials
            {
                Email = email,
                Password = null,
                UserId = row.GetValue<Guid>("userid")
            };
        }

        private async Task<RowSet> ExecuteMultiple(string key, PreparedStatement ps, params object[] values)
        {
            var statement = ps.Bind(values);
            var maxInFlight = _semaphore.CurrentCount;
            var counter = new SendReceiveCounter();
            var tcs = new TaskCompletionSource<RowSet>();
            for (var i = 0; i < maxInFlight; i++)
            {
                SendNew(key, statement, tcs, counter);
            }
            return await tcs.Task;
        }

        private void SendNew(string key, IStatement statement, TaskCompletionSource<RowSet> tcs, SendReceiveCounter counter)
        {
            if (counter.IncrementSent() > _repeatLength)
            {
                return;
            }
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            //var t1 = Task.Run(async () => await _session.ExecuteAsync(statement));
            var t1 = _session.ExecuteAsync(statement);
            t1.ContinueWith(t =>
            {
                stopWatch.Stop();
                if (t.Exception != null)
                {
                    tcs.TrySetException(t.Exception.InnerException);
                    return;
                }
                _metrics.Update(key, stopWatch.ElapsedMilliseconds);
                var received = counter.IncrementReceived();
                if (received == _repeatLength)
                {
                    tcs.TrySetResult(t.Result);
                    return;
                }
                SendNew(key, statement, tcs, counter);
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        public async Task<TimeUuid> Now()
        {
            var rs = await _session.ExecuteAsync(new SimpleStatement("SELECT NOW() FROM system.local"));
            return rs.First().GetValue<TimeUuid>(0);
        }

        private class SendReceiveCounter
        {
            private int _receiveCounter;
            private int _sendCounter;

            public int IncrementSent()
            {
                return Interlocked.Increment(ref _sendCounter);
            }

            public int IncrementReceived()
            {
                return Interlocked.Increment(ref _receiveCounter);
            }
        }
    }
}
