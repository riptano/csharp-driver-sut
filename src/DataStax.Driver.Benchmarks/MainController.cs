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
using DataStax.Driver.Benchmarks.Models;

namespace DataStax.Driver.Benchmarks
{
    public class MainController : ApiController
    {
        private readonly Repository _repository;

        public MainController(Repository repository)
        {
            _repository = repository;
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

        public IHttpActionResult JsonTest(UserCredentials credentials)
        {
            return Ok(credentials);
        }

        public async Task<IHttpActionResult> InsertCredentials(UserCredentials credentials)
        {
            await _repository.Insert(credentials);
            return Ok(credentials);
        }
    }
}
