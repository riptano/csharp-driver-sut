using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassandra;

namespace DataStax.Driver.Benchmarks.Profiles
{
    public interface IProfile
    {
        /// <summary>
        /// Initialization and warmup
        /// </summary>
        Task Init(ISession session, Options options);
        Task Insert();
        Task Select();
    }
}
