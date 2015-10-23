using System;
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
            public const string SelectCredentials = "SELECT email, password, userid FROM killrvideo.user_credentials WHERE email = ?";
        }

        private readonly ISession _session;
        private readonly PreparedStatement _insertPs;
        private readonly PreparedStatement _selectPs;

        public Repository(ISession session)
        {
            _session = session;
            _insertPs = session.Prepare(Queries.InsertCredentials);
            _selectPs = session.Prepare(Queries.SelectCredentials);
        }

        public async Task Insert(UserCredentials credentials)
        {
            credentials.UserId = Guid.NewGuid();
            await _session.ExecuteAsync(_insertPs.Bind(credentials.Email, credentials.Password, credentials.UserId));
        }

        public IStatement[] Preallocate<T>(int length)
        {
            if (typeof(T) == typeof(UserCredentials))
            {
                var items = new IStatement[length];
                for (var i = 0; i < length; i++)
                {
                    var id = Guid.NewGuid();
                    items[i] = _insertPs.Bind(i.ToString(), i.ToString(), id);
                }
                return items;
            }
            throw new NotSupportedException(string.Format("Type {0} is not supported", typeof(T)));
        }

        public async Task<long> Execute(IStatement[] statements)
        {
            var semaphore = new SemaphoreSlim(512);
            var tasks = new Task<RowSet>[statements.Length];
            var watch = new Stopwatch();
            watch.Start();
            for (var i = 0; i < statements.Length; i++)
            {
                await semaphore.WaitAsync();
                var index = i;
                var stmt = statements[index];
                tasks[index] = Task.Run(async () =>
                {
                    RowSet rs;
                    try
                    {
                        rs = await _session.ExecuteAsync(stmt);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                    return rs;
                });
            }
            await Task.WhenAll(tasks);
            watch.Stop();
            return watch.ElapsedMilliseconds;
        }
    }
}
