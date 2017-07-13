using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dse;

namespace DataStax.Driver.Benchmarks.Profiles
{
    class DseStandardProfile : StandardProfile
    {
        protected ISession Session;
        protected PreparedStatement InsertPs;
        protected PreparedStatement SelectPs;

        public DseStandardProfile(ISession session)
        {
            Session = session;
        }

        protected override void PrepareStatements()
        {
            if (InsertQuery != null)
            {
                Task.Run(async () =>
                {
                    InsertPs = await Session.PrepareAsync(InsertQuery);
                }).Wait();
            }
            if (SelectQuery != null)
            {
                Task.Run(async () =>
                {
                    SelectPs = await Session.PrepareAsync(SelectQuery);
                }).Wait();
            }
        }

        protected override Task ExecuteAsync(string query)
        {
            return Session.ExecuteAsync(new SimpleStatement(query));
        }

        protected override Task PrepareAsync(string query)
        {
            return Session.PrepareAsync(query);
        }

        protected override Task ExecuteInsertAsync(long index)
        {
            var val = BitConverter.GetBytes(index);
            var insertBinded = InsertPs.Bind(val, val, val, val, val, val);
            return Session.ExecuteAsync(insertBinded);
        }

        protected override Task ExecuteSelectAsync(long index)
        {
            var val = BitConverter.GetBytes(index);
            return Session.ExecuteAsync(SelectPs.Bind(val));
        }
    }
}
