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
    internal class Repository
    {
        private static class Queries
        {
            public const string InsertCredentials = "INSERT INTO killrvideo.user_credentials (email, password, userid) VALUES (?, ?, ?)";
            public const string SelectCredentials = "SELECT email, password, userid FROM killrvideo.user_credentials WHERE email = ?";
        }

        private readonly ISession _session;
        private readonly IMetricsTracker _metrics;
        private readonly int _parallelism;
        private readonly int _maxOutstandingRequests;
        private readonly PreparedStatement _insertPs;
        private readonly PreparedStatement _selectPs;

        public Repository(ISession session, IMetricsTracker metrics, int parallelism, int maxOutstandingRequests)
        {
            _session = session;
            _metrics = metrics;
            _parallelism = parallelism;
            _maxOutstandingRequests = maxOutstandingRequests;
            _insertPs = session.Prepare(Queries.InsertCredentials);
            _selectPs = session.Prepare(Queries.SelectCredentials);
        }

        public async Task Insert(UserCredentials credentials)
        {
            credentials.UserId = Guid.NewGuid();
            await _session.ExecuteAsync(_insertPs.Bind(credentials.Email, credentials.Password, credentials.UserId));
        }

        public IStatement[] Preallocate<T>(bool insert, int length)
        {
            var items = new IStatement[length];
            if (typeof(T) == typeof(UserCredentials))
            {
                if (insert)
                {
                    for (var i = 0; i < length; i++)
                    {
                        var id = Guid.NewGuid();
                        items[i] = _insertPs.Bind(i.ToString(), i.ToString(), id);
                    }
                }
                else
                {
                    for (var i = 0; i < length; i++)
                    {
                        items[i] = _selectPs.Bind(i.ToString());
                    }
                }
                return items;
            }
            throw new NotSupportedException(string.Format("Type {0} is not supported", typeof(T)));
        }

        public async Task<long> Execute<T>(IStatement[] statements, bool fetchFirst)
        {
            //Start launching in parallel
            var semaphore = new SemaphoreSlim(_maxOutstandingRequests);
            var tasks = new Task<RowSet>[statements.Length];
            var chunkSize = statements.Length / _parallelism;
            if (chunkSize == 0)
            {
                chunkSize = 1;
            }
            Action<RowSet> fetch = rs =>
            {
                var row = rs.FirstOrDefault();
                if (row != null)
                {
                    row.GetValue<T>(0);
                }
            };
            if (!fetchFirst)
            {
                fetch = _ => { };
            }
            var statementLength = statements.Length;
            var launchTasks = new Task[_parallelism + 1];
            for (var i = 0; i < _parallelism + 1; i++)
            {
                var startIndex = i * chunkSize;
                launchTasks[i] = Task.Run(async () =>
                {
                    for (var j = 0; j < chunkSize; j++)
                    {
                        var index = startIndex + j;
                        if (index >= statementLength)
                        {
                            break;
                        }
                        await semaphore.WaitAsync();
                        var t = _session.ExecuteAsync(statements[index]);
                        tasks[index] = t;
                        var rs = await t;
                        semaphore.Release();
                        fetch(rs);
                    }
                });
            }
            var watch = new Stopwatch();
            watch.Start();
            await Task.WhenAll(launchTasks);
            await Task.WhenAll(tasks);
            watch.Stop();
            return watch.ElapsedMilliseconds;
        }
    }
}
