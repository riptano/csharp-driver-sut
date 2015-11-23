using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using System.Web.Http.Routing;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Owin;

namespace DataStax.Driver.Benchmarks
{
    internal class WebStartup
    {
        public static void Build(Repository repository, IAppBuilder appBuilder)
        {
            var config = new HttpConfiguration();
            config.Routes.MapHttpRoute("Home", "", new { controller = "Main", action = "Index" });
            config.Routes.MapHttpRoute("Now", "cassandra", new { controller = "Main", action = "Now" });
            config.Routes.MapHttpRoute("JsonTest", "json-test", new
            {
                controller = "Main",
                action = "JsonTest"
            }, new
            {
                httpMethod = new HttpMethodConstraint(HttpMethod.Post)
            });
            config.Routes.MapHttpRoute("Insert-Prepared", "prepared-statements/credentials", new
            {
                controller = "Main",
                action = "InsertCredentials"
            }, new
            {
                httpMethod = new HttpMethodConstraint(HttpMethod.Post)
            });
            config.Routes.MapHttpRoute("Get-Prepared", "prepared-statements/credentials/{email}", new
            {
                controller = "Main",
                action = "GetCredentials"
            }, new
            {
                httpMethod = new HttpMethodConstraint(HttpMethod.Get)
            });
            //single instance of Repository
            //poor man's DI
            config.Services.Replace(typeof(IHttpControllerActivator), new SingleControllerActivator(repository));
            var jsonSettings = config.Formatters.JsonFormatter.SerializerSettings;
            jsonSettings.Formatting = Formatting.Indented;
            //Use camel case json
            jsonSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            appBuilder.UseWebApi(config);
        }
    }

    internal class SingleControllerActivator : IHttpControllerActivator
    {
        private readonly Repository _repository;

        public SingleControllerActivator(Repository repository)
        {
            _repository = repository;
        }

        public IHttpController Create(HttpRequestMessage request, HttpControllerDescriptor controllerDescriptor, Type controllerType)
        {
            return new MainController(_repository);
        }
    }
}
