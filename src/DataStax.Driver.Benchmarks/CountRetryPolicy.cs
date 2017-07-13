using System.Threading;
using Cassandra;

namespace DataStax.Driver.Benchmarks
{
    public class CountRetryPolicy : IRetryPolicy, Dse.IRetryPolicy
    {
        private long _countRead;
        private long _countWrite;
        private long _countUnavailable;

        public long GetReadCount()
        {
            return Interlocked.Exchange(ref _countRead, 0L);
        }

        public long GetWriteCount()
        {
            return Interlocked.Exchange(ref _countWrite, 0L);
        }

        public long GetUnavailableCount()
        {
            return Interlocked.Exchange(ref _countUnavailable, 0L);
        }

        public RetryDecision OnReadTimeout(IStatement query, ConsistencyLevel cl, int requiredResponses, int receivedResponses,
            bool dataRetrieved, int nbRetry)
        {
            Interlocked.Increment(ref _countRead);
            return RetryDecision.Ignore();
        }

        public RetryDecision OnWriteTimeout(IStatement query, ConsistencyLevel cl, string writeType, int requiredAcks, int receivedAcks,
            int nbRetry)
        {
            Interlocked.Increment(ref _countWrite);
            return RetryDecision.Ignore();
        }

        public RetryDecision OnUnavailable(IStatement query, ConsistencyLevel cl, int requiredReplica, int aliveReplica, int nbRetry)
        {
            Interlocked.Increment(ref _countUnavailable);
            return RetryDecision.Ignore();
        }

        public Dse.RetryDecision OnReadTimeout(Dse.IStatement query, Dse.ConsistencyLevel cl, int requiredResponses, int receivedResponses,
            bool dataRetrieved, int nbRetry)
        {
            Interlocked.Increment(ref _countRead);
            return Dse.RetryDecision.Ignore();
        }

        public Dse.RetryDecision OnWriteTimeout(Dse.IStatement query, Dse.ConsistencyLevel cl, string writeType, int requiredAcks, int receivedAcks,
            int nbRetry)
        {
            Interlocked.Increment(ref _countWrite);
            return Dse.RetryDecision.Ignore();
        }

        public Dse.RetryDecision OnUnavailable(Dse.IStatement query, Dse.ConsistencyLevel cl, int requiredReplica, int aliveReplica, int nbRetry)
        {
            Interlocked.Increment(ref _countUnavailable);
            return Dse.RetryDecision.Ignore();
        }
    }
}