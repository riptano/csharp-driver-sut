using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStax.Driver.Benchmarks.Models
{
    internal class StandardPoco
    {
        public byte[] Key { get; set; }

        public byte[] C0 { get; set; }

        public byte[] C1 { get; set; }

        public byte[] C2 { get; set; }

        public byte[] C3 { get; set; }

        public byte[] C4 { get; set; }
    }
}
