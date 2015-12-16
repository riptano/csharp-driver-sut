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
        public async Task<HttpResponseMessage> Now()
        {
            var time = await _repository.Now();
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Now: " + time, Encoding.UTF8, "text/plain")
            };
            return resp;
        }

        public async Task<IHttpActionResult> InsertCredentials(UserCredentials credentials)
        {
            await _repository.Insert(credentials);
            return Ok(credentials);
        }

        public async Task<IHttpActionResult> GetCredentials(string email)
        {
            var credentials = await _repository.GetCredentials(email);
            return Json(credentials);
        }

        public async Task<IHttpActionResult> InsertVideo(Video video)
        {
            await _repository.Insert(video);
            return Ok(video);
        }

        public async Task<IHttpActionResult> GetVideo(Guid videoId)
        {
            var video = await _repository.GetVideo(videoId);
            return Json(video);
        }

        public async Task<IHttpActionResult> InsertVideoEvent(VideoEvent videoEvent)
        {
            await _repository.Insert(videoEvent);
            return Ok(videoEvent);
        }

        public async Task<IHttpActionResult> GetVideoEvent(Guid videoId, Guid userId)
        {
            var video = await _repository.GetVideoEvent(videoId, userId);
            return Json(video);
        }
    }
}
