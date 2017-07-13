using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStax.Driver.Benchmarks
{
    interface ITestScript
    {
        Task Run(Options options);
    }
}
