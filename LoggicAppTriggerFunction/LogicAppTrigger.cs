
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace LoggicAppTriggerFunction
{
    public static class LogicAppTrigger
    {
        private static readonly Lazy<string> logicAppPath = new Lazy<string>(() => Environment.GetEnvironmentVariable("LogicAppPath"));

        [FunctionName("LogicAppTrigger")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req, ILogger log)
        {
            using (var client = new HttpClient())
            {
                var result = await PoolingRequestToLogicApp(client, logicAppPath.Value);
                return req.CreateResponse(result);
            }
        }

        private static async Task<HttpStatusCode> PoolingRequestToLogicApp(HttpClient client, string path)
        {
            try
            {
                var response = await client.GetAsync(path);

                if (IsAccepted(response))
                {
                    await Task.Delay(response.Headers.RetryAfter.Delta ?? TimeSpan.FromSeconds(10));
                    return await PoolingRequestToLogicApp(client, response.Headers.Location.ToString());
                }

                return response.StatusCode;
            }
            catch (Exception)
            {
                return HttpStatusCode.InternalServerError;
            }
        }

        private static bool IsAccepted(HttpResponseMessage response)
        {
            return response.StatusCode == HttpStatusCode.Accepted;
        }
    }
}
