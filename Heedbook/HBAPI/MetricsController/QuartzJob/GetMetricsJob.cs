using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using HBLib;
using HBLib.Utils;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;
using Attachment = HBLib.Utils.Attachment;


namespace MetricsController.QuartzJob
{
    public class GetMetricsJob : IJob
    {
        private AzureConnector _connector;
        private IServiceScopeFactory _scopeFactory;
        private readonly ElasticClientFactory _elasticClientFactory;
        private SlackClient _slackClient;

        public GetMetricsJob(IServiceScopeFactory scopeFactory,
            ElasticClientFactory elasticClientFactory,
            SlackClient slackClient)
        {
            _scopeFactory = scopeFactory;
            _elasticClientFactory = elasticClientFactory;
            _slackClient = slackClient;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var _log = _elasticClientFactory.GetElasticClient();
            try
            {
                _log.Info($"Start function");
                using (var scope = _scopeFactory.CreateScope())
                {
                    _connector = scope.ServiceProvider.GetRequiredService<AzureConnector>();
                    _connector._log = _log;
                    var metrics = _connector.GetMetrics();
                    var payload = new Payload()
                    {
                        Attachments = new List<Attachment>()
                    };
                        foreach (var metric in metrics)
                        {
                            var attachment = new Attachment()
                            {
                                Color = "#36a64f",
                                Pretext = $"{metric.VmName}",
                                Fields = new List<Field>()
                            };
                            foreach (var metricValue in metric.MetricValues)
                            {
                                var field = new Field()
                                {
                                    Title = $"{metricValue.Name}",
                                    Value = $"Max:{metricValue.Max} {metricValue.Unit} Average:{metricValue.Average} {metricValue.Unit}",
                                    Short = false,
                                    
                                };
                                attachment.Fields.Add(field);
                            }
                            payload.Attachments.Add(attachment);

                        };
                    
                    
                    _slackClient.PostMessage(payload);
                }
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occured:{e}");
            }

            _log.Info($"Finished function");
        }
    }
}