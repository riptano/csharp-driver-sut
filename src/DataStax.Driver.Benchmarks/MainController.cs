using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Cassandra;
#pragma warning disable 618

namespace DataStax.Driver.Benchmarks
{
    public class MainController : ApiController
    {
        private static Statement GetInsertSimpleStatement(object[] values)
        {
            return new SimpleStatement(Program.InsertQuery).Bind(values);
        }

        private static Statement GetInsertBoundStatement(object[] values)
        {
            return Program.InsertPs.Bind(values);
        }

        private static Statement GetSelectSimpleStatement(object[] values)
        {
            return new SimpleStatement(Program.SelectQuery).Bind(values);
        }

        private static Statement GetSelectBoundStatement(object[] values)
        {
            return Program.SelectPs.Bind(values);
        }

        [HttpGet]
        public HttpResponseMessage Index()
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Hello World!", Encoding.UTF8, "text/plain")
            };
            return resp;
        }

        [HttpGet]
        public HttpResponseMessage Now()
        {
            var row = Program.Session.Execute("SELECT NOW() FROM system.local").First();
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Now: " + row.GetValue<TimeUuid>("NOW()"), Encoding.UTF8, "text/plain")
            };
            return resp;
        }

        [HttpPost]
        public async Task<HttpResponseMessage> Insert(int start, int length, bool prepared)
        {
            var tasks = new Task[length];
            Func<object[], Statement> getStmt = GetInsertSimpleStatement;
            if (prepared)
            {
                getStmt = GetInsertBoundStatement;
            }
            for (var i = 0; i < length; i++)
            {
                var id = (start + i).ToString();
                var values = new object[] { "user-" + id, "first-" + id, "last-" + id, "pass", new List<string>(new[] { id + "@datastax.com" }), DateTimeOffset.Now };
                tasks[i] = Program.Session.ExecuteAsync(getStmt(values));
            }
            await Task.WhenAll(tasks);
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("OK", Encoding.UTF8, "text/plain")
            };
            return resp;
        }

        [HttpGet]
        public async Task<HttpResponseMessage> Select(int start, int length, bool prepared)
        {
            var tasks = new Task<RowSet>[length];
            Func<object[], Statement> getStmt = GetSelectSimpleStatement;
            if (prepared)
            {
                getStmt = GetSelectBoundStatement;
            }
            for (var i = 0; i < length; i++)
            {
                var values = new object[] { "user-" + (start + i).ToString()};
                tasks[i] = Program.Session.ExecuteAsync(getStmt(values));
            }
            var results = await Task.WhenAll(tasks);
            var usernames = results
                .Select(rs =>
                {
                    var row = rs.FirstOrDefault();
                    if (row == null)
                    {
                        return null;
                    }
                    return row.GetValue<string>("username");
                })
                .Where(v => v != null);
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(String.Join(",", usernames), Encoding.UTF8, "text/plain")
            };
            return resp;
        }
    }
}
