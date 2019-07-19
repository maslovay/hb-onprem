using Microsoft.Azure.Management.Monitor.Fluent;
using Microsoft.Azure.Management.Monitor.Fluent.Models;
using Microsoft.Rest.Azure.Authentication;
using Microsoft.Rest.Azure.OData;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HBLib;
using Newtonsoft.Json;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using MetricsController.Controllers;

namespace MetricsController
{
    public class AzureConnector
    {
        private AzureSettings _settings;

        public AzureConnector(AzureSettings settings)
        {
            _settings = settings;
        }
        
        public IEnumerable<Metrics> GetMetrics()
        {
            var metrics = new[] { "Percentage CPU", "Disk Write Operations/Sec", "Disk Read Operations/Sec", "Disk Read Bytes", "Disk Write Bytes" };
            Microsoft.Azure.Management.Fluent.IAzure azure = AuthenticateWithMonitoringClient().Result;
            var vms = azure.VirtualMachines.ListByResourceGroup("HBONPREMTEST");
            var metricsList = new List<Metrics>();
            foreach (var vm in vms.Where(item => item.Name.Contains("Slave")))
            {
                var metricDefinitions = azure.MetricDefinitions.ListByResource(vm.Id)
                    .Where(item => metrics.Any(i => i == item.Name.Value));
                var metric = new Metrics()
                {
                    VmName = vm.Name,
                    Time = DateTime.Now,
                    MetricsProp = new List<Metricsprop>()
                };
                foreach (var metricDefinition in metricDefinitions)
                {
                    DateTime recordDateTime = DateTime.UtcNow;
                    var metricCollection = metricDefinition.DefineQuery()
                    .StartingFrom(recordDateTime.AddMinutes(-15))
                    .EndsBefore(recordDateTime)
                    .WithAggregation("Maximum, Average")
                    .WithInterval(TimeSpan.FromMinutes(15))
                    .Execute();
                    metric.MetricsProp.Add(new Metricsprop()
                    {
                        Name = metricCollection.Metrics[0].Name.Value,
                        Unit = metricCollection.Metrics[0].Unit.ToString(),
                        Average = metricCollection.Metrics[0].Timeseries[0].Data[0].Average.Value.ToString(CultureInfo.InvariantCulture),
                        Max = metricCollection.Metrics[0].Timeseries[0].Data[0].Maximum.Value.ToString(CultureInfo.InvariantCulture)
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

