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
#pragma warning disable 4014

namespace DataStax.Driver.Benchmarks
{
    public class Repository
    {
        private static class Queries
        {
            public const string CredentialsInsert = "INSERT INTO killrvideo.user_credentials (email, password, userid) VALUES (?, ?, ?)";
            public const string CredentialsSelect = "SELECT userid FROM killrvideo.user_credentials WHERE email = ?";
            public const string VideoInsert = "INSERT INTO killrvideo.videos (videoid, userid, name, description, location, location_type," +
                                              " preview_thumbnails, tags, added_date) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)";
            public const string VideoSelect = "SELECT videoid, userid, name, description, location, location_type, preview_thumbnails," +
                                              " tags, added_date FROM killrvideo.videos WHERE videoid = ?";
            public const string VideoEventInsert = "INSERT INTO killrvideo.video_event (videoid, userid, event, event_timestamp, video_timestamp)" +
                                                   " VALUES (?, ?, ?, ?, ?)";
            public const string VideoEventSelect = "SELECT event, event_timestamp, video_timestamp FROM killrvideo.video_event" +
                                                   " WHERE videoid = ? AND userid = ?";
        }

        private readonly ISession _session;
        private readonly IMetricsTracker _metrics;
        private readonly bool _globallyLimited;
        private readonly SemaphoreSlim _semaphore;
        private readonly int _repeatLength;
        private readonly PreparedStatement _credentialsInsertPs;
        private readonly PreparedStatement _credentialsSelectPs;
        private readonly PreparedStatement _videoInsertPs;
        private readonly PreparedStatement _videoSelectPs;
        private readonly PreparedStatement _videoEventInsertPs;
        private readonly PreparedStatement _videoEventSelectPs;
        private readonly int _maxInFlight;

        public Repository(ISession session, IMetricsTracker metrics, bool globallyLimited, Options options)
        {
            _session = session;
            _metrics = metrics;
            _globallyLimited = globallyLimited;
            _maxInFlight = options.MaxOutstandingRequests * session.Cluster.AllHosts().Count;
            _semaphore = new SemaphoreSlim(_maxInFlight);
            _repeatLength = options.CqlRequests;
            _credentialsInsertPs = session.Prepare(Queries.CredentialsInsert);
            _credentialsSelectPs = session.Prepare(Queries.CredentialsSelect);
            _videoInsertPs = session.Prepare(Queries.VideoInsert);
            _videoSelectPs = session.Prepare(Queries.VideoSelect);
            _videoEventInsertPs = session.Prepare(Queries.VideoEventInsert);
            _videoEventSelectPs = session.Prepare(Queries.VideoEventSelect);
        }

        public async Task Insert(UserCredentials credentials)
        {
            credentials.UserId = Guid.NewGuid();
            await Execute("prepared-insert-user_credentials", _credentialsInsertPs, credentials.Email, credentials.Password, credentials.UserId);
        }

        public async Task Insert(Video video)
        {
            if (video.VideoId == Guid.Empty)
            {
                video.VideoId = Guid.NewGuid();
            }
            await Execute("prepared-insert-video", _videoInsertPs, video.VideoId, video.UserId, video.Name, video.Description,
                video.Location, video.LocationType, video.PreviewThumbnails, video.Tags, video.AddedDate);
        }

        public async Task Insert(VideoEvent videoEvent)
        {
            await Execute("prepared-insert-video_event", _videoEventInsertPs, videoEvent.VideoId, videoEvent.UserId, videoEvent.Event, 
                videoEvent.EventTimestamp, videoEvent.VideoTimestamp);
        }

        public async Task<UserCredentials> GetCredentials(string email)
        {
            var rs = await Execute("prepared-select-user_credentials", _credentialsSelectPs, email);
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

        public async Task<Video> GetVideo(Guid videoId)
        {
            var rs = await Execute("prepared-select-video", _videoSelectPs, videoId);
            var row = rs.FirstOrDefault();
            if (row == null)
            {
                return null;
            }
            return new Video
            {
                VideoId = videoId,
                UserId = row.GetValue<Guid>("userid"),
                Name = row.GetValue<string>("name"),
                Description = row.GetValue<string>("description"),
                Location = row.GetValue<string>("location"),
                LocationType = row.GetValue<int>("location_type"),
                PreviewThumbnails = row.GetValue<SortedDictionary<string, string>>("preview_thumbnails"),
                Tags = row.GetValue<string[]>("tags"),
                AddedDate = row.GetValue<DateTimeOffset>("added_date")
            };
        }

        public async Task<VideoEvent> GetVideoEvent(Guid videoId, Guid userId)
        {
            var rs = await Execute("prepared-select-video_event", _videoEventSelectPs, videoId, userId);
            var row = rs.FirstOrDefault();
            if (row == null)
            {
                return null;
            }
            return new VideoEvent
            {
                VideoId = videoId,
                UserId = userId,
                Event = row.GetValue<string>("event"),
                EventTimestamp = row.GetValue<TimeUuid>("event_timestamp"),
                VideoTimestamp = row.GetValue<long>("video_timestamp")
            };
        }

        private async Task<RowSet> Execute(string key, PreparedStatement ps, params object[] values)
        {
            if (_globallyLimited)
            {
                return await ExecuteMultipleGlobal(key, ps, values);
            }
            return await ExecuteMultiple(key, ps, values);
        }

        /// <summary>
        /// Executes n (_maxInFlight) statements in parallel, a total of m (_repeatLength) times.
        /// </summary>
        private async Task<RowSet> ExecuteMultiple(string key, PreparedStatement ps, params object[] values)
        {
            var statement = ps.Bind(values);
            var counter = new Utils.SendReceiveCounter();
            var tcs = new TaskCompletionSource<RowSet>();
            for (var i = 0; i < _maxInFlight; i++)
            {
                SendNew(key, statement, tcs, counter);
            }
            return await tcs.Task;
        }

        /// <summary>
        /// Executes a new statement.
        /// Each time a Statement finished executing, starts a new one until received == repeatLength.
        /// </summary>
        private void SendNew(string key, IStatement statement, TaskCompletionSource<RowSet> tcs, Utils.SendReceiveCounter counter)
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

        /// <summary>
        /// Executes m (_repeatLength) statements in parallel, globally limited by the semaphore
        /// </summary>
        private async Task<RowSet> ExecuteMultipleGlobal(string key, PreparedStatement ps, params object[] values)
        {
            var statement = ps.Bind(values);
            var counter = new Utils.SendReceiveCounter();
            var tcs = new TaskCompletionSource<RowSet>();
            for (var i = 0; i < _repeatLength; i++)
            {
                await _semaphore.WaitAsync();
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                //var t1 = Task.Run(async () => await _session.ExecuteAsync(statement));
                var t1 = _session.ExecuteAsync(statement);
                t1.ContinueWith(t =>
                {
                    stopWatch.Stop();
                    _semaphore.Release();
                    if (t.Exception != null)
                    {
                        tcs.TrySetException(t.Exception.InnerException);
                        return;
                    }
                    _metrics.Update(key, stopWatch.ElapsedMilliseconds);
                    var received = counter.IncrementReceived();
                    if (received == _repeatLength)
                    {
                        //Mark this as finished
                        tcs.TrySetResult(t.Result);
                    }
                }, TaskContinuationOptions.ExecuteSynchronously);
            }
            return await tcs.Task;
        }

        public async Task<TimeUuid> Now()
        {
            var rs = await _session.ExecuteAsync(new SimpleStatement("SELECT NOW() FROM system.local"));
            return rs.First().GetValue<TimeUuid>(0);
        }
    }
}
