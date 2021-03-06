using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Web.Http;
using Elasticsearch.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;


namespace LogRelay
{
    public static class LogIt
    {
        private static ElasticLowLevelClient _client = null;

        private static ElasticLowLevelClient Client => (_client ?? (_client = new ElasticLowLevelClient(new ConnectionConfiguration(new Uri(Environment.GetEnvironmentVariable("ElasticSearchUrl"))))));

        [FunctionName("LogIt")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            string index = Environment.GetEnvironmentVariable("ElasticSearchIndex");

            if (string.IsNullOrWhiteSpace(index))
                return new InternalServerErrorResult();
            
            string requestBody = new StreamReader(req.Body).ReadToEnd();

            dynamic obj = JsonConvert.DeserializeObject(requestBody);

            index = index.Trim('-', '*') + '-' + DateTime.UtcNow.ToString("yyyy.MM.dd");

            List<int> codes = new List<int>();

            foreach (var message in obj)
            {
                var resp = Client.CreatePost<DynamicResponse>(index, "log", Guid.NewGuid().ToString(),
                    message.ToString());
                codes.Add(resp.HttpStatusCode ?? 0);
            }

            return new OkObjectResult(codes);
        }
        
    }
}
