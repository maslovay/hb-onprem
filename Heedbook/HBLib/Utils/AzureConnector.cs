using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent;

namespace HBLib.Utils
{
    public class AzureConnector
    {
        public ElasticClient _log { get; set; }
        private AzureSettings _settings;
        private ElasticClientFactory _elasticClientFactory;

        public AzureConnector(AzureSettings settings,ElasticClientFactory elasticClientFactory)
        {
            _settings = settings;
            _elasticClientFactory = elasticClientFactory;
        }
        
        public IEnumerable<Metrics> GetMetrics()
        {
            
            _log.Info("Try to login in azure");
            Microsoft.Azure.Management.Fluent.IAzure azure = AuthenticateWithMonitoringClient().Result;
            var vms = azure.VirtualMachines.ListByResourceGroup(_settings.ResourceGroup);
            var metricsList = new List<Metrics>();
            _log.Info($"Getting Metrics");
            foreach (var vm in vms.Where(item => _settings.VmNames.Any(i=> i == item.Name)))
            {
                var metricDefinitions = azure.MetricDefinitions.ListByResource(vm.Id)
                    .Where(item => _settings.Metrics.Any(i => i == item.Name.Value));
                var metric = new Metrics()
                {
                    VmName = vm.Name,
                    Time = DateTime.Now,
                    MetricValues = new List<MetricValue>()
                };
                foreach (var metricDefinition in metricDefinitions)
                {
                    DateTime recordDateTime = DateTime.UtcNow;
                    var metricCollection = metricDefinition.DefineQuery()
                    .StartingFrom(recordDateTime.AddMinutes(-30))
                    .EndsBefore(recordDateTime)
                    .WithAggregation("Maximum, Average")
                    .WithInterval(TimeSpan.FromMinutes(30))
                    .Execute();
                    metric.MetricValues.Add(new MetricValue()
                    {
                        Name = metricCollection.Metrics[0].Name.Value,
                        Unit = metricCollection.Metrics[0].Unit.ToString(),
                        Average = (int)Math.Round(metricCollection.Metrics[0].Timeseries[0].Data[0].Average.Value),
                        Max = (int)Math.Round(metricCollection.Metrics[0].Timeseries[0].Data[0].Maximum.Value)
                        
                    });
                }
                metricsList.Add(metric);
            }

            return metricsList;

        }
        private  async Task<Microsoft.Azure.Management.Fluent.IAzure> AuthenticateWithMonitoringClient()
            {
                var credentials = SdkContext.AzureCredentialsFactory
                                .FromServicePrincipal(_settings.ClientId,
                                _settings.Secret,
                                _settings.TenantId,
                                AzureEnvironment.AzureGlobalCloud);
                var azure = Microsoft.Azure.Management.Fluent.Azure
                    .Configure()
                    .Authenticate(credentials)
                    .WithDefaultSubscription();
                return azure;
            }
        }
    }

