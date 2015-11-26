﻿using System;
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
        private readonly int _parallelism;
        private readonly int _repeatLength;
        private readonly PreparedStatement _insertPs;
        private readonly PreparedStatement _selectPs;

        public Repository(ISession session, IMetricsTracker metrics, Options options)
        {
            _session = session;
            _metrics = metrics;
            _semaphore = new SemaphoreSlim(options.MaxOutstandingRequests * session.Cluster.AllHosts().Count);
            _parallelism = options.Parallelism;
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
            var chunkSize = Convert.ToInt32((double)_repeatLength / _parallelism);
            if (chunkSize == 0)
            {
                chunkSize = 1;
            }
            var launchTasks = new Task[_parallelism + 1];
            var tasks = new Task<RowSet>[_repeatLength];
            for (var i = 0; i < _parallelism + 1; i++)
            {
                var startIndex = i * chunkSize;
                launchTasks[i] = Task.Run(async () =>
                {
                    for (var j = 0; j < chunkSize; j++)
                    {
                        var index = startIndex + j;
                        if (index >= _repeatLength)
                        {
                            break;
                        }
                        var stopWatch = new Stopwatch();
                        await _semaphore.WaitAsync();
                        stopWatch.Start();
                        var t = _session.ExecuteAsync(statement);
                        tasks[index] = t;
                        await t;
                        stopWatch.Stop();
                        _semaphore.Release();
                        _metrics.Update(key, stopWatch.ElapsedMilliseconds);
                    }
                });

            }
            await Task.WhenAll(launchTasks);
            await Task.WhenAll(tasks);
            return tasks[0].Result;
        }

        public async Task<TimeUuid> Now()
        {
            var rs = await _session.ExecuteAsync(new SimpleStatement("SELECT NOW() FROM system.local"));
            return rs.First().GetValue<TimeUuid>(0);
        }
    }
}
