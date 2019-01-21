using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HBLib.Utils;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace OperationService.Legacy
{
    public static class SlackHttpAlertWebhookBridge
    {
        private static HttpClient httpClient = new HttpClient();

        [FunctionName("Slack_Http_AlertWebhookBridge")]
        public static async Task<HttpResponseMessage> Run(
            HttpRequestMessage req,
            ILogger log,
            ExecutionContext dir)
        {
            try
            {
                var slackWebhookAlertURL = EnvVar.Get("SlackWebhookAlertURL");
                var appInsightsIconrUrl = EnvVar.Get("SlackIconURL");
                ;

                // Get request body
                dynamic js = await req.Content.ReadAsAsync<object>();

                string color;

                if (js["status"] == "Activated")
                {
                    // red
                    color = "#e01563";
                }
                else if (js["status"] == "Resolved")
                {
                    // green
                    color = "#36a64f";
                }
                else
                {
                    // blue
                    color = "#6ecadc";
                }

                string name = js["context"]["name"];
                string status = js["status"];
                string portalLink = js["context"]["portalLink"];
                string description = js["context"]["description"];
                string resourceGroupName = js["context"]["resourceGroupName"];
                string resourceName = js["context"]["resourceName"];
                string conditionType = js["context"]["conditionType"];
                string datetime = "";

                string js_str = $@"{{
                    'attachments': [
                        {{
                            'fallback': 'Alert ""{name}"" is ""{status}""',
                            'color': '{color}',
                            'title': '{name}',
                            'title_link': '{portalLink}',
                            'text': '{description}',
                            'author_name': 'Application Insights',
                            'author_icon': '{appInsightsIconrUrl}',
                            'fields': [
                                {{
                                    'title': 'Resource Group',
                                    'value': '{resourceGroupName}',
                                    'short': true
                                }},
				                {{
                                    'title': 'Resource Name',
                                    'value': '{resourceName}',
                                    'short': true
                                }},
			                    {{
                                    'title': 'Condition Type',
                                    'value': '{conditionType}',
                                    'short': true
                                }},
                                {{
                                    'title': 'Status',
                                    'value': '{status}',
                                    'short': true
                                }}
				
                            ],
			                'footer': '{datetime}'}}
                    ]
                }}";

                httpClient.DefaultRequestHeaders.Accept.Clear();
                dynamic message = JsonConvert.DeserializeObject(js_str);

                var response = await httpClient.PostAsync(slackWebhookAlertURL,
                    new StringContent(JsonConvert.SerializeObject(message).ToString(), Encoding.UTF8,
                        "application/json"));
                var resreq = await response.Content.ReadAsStringAsync();
                log.LogInformation($"Response: {resreq}");

                var res = new HttpResponseMessage(HttpStatusCode.OK);

                log.LogInformation($"Function finished: {dir.FunctionName}");
                return res;
            }
            catch (Exception e)
            {
                log.LogError($"Exception occured {e}");
                throw;
            }
        }
    }
}