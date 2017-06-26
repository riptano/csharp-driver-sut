using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassandra;
using DataStax.Driver.Benchmarks.Metrics;

namespace DataStax.Driver.Benchmarks.Profiles
{
    public interface IProfile
    {
        /// <summary>
        /// Initialization and warmup
        /// </summary>
        Task Init(Options options);
        Task<Timer> Insert();
        Task<Timer> Select();
    }
}
