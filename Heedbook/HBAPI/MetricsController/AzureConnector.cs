using Microsoft.Azure.Management.Monitor.Fluent;
using Microsoft.Azure.Management.Monitor.Fluent.Models;
using Microsoft.Rest.Azure.Authentication;
using Microsoft.Rest.Azure.OData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent;

namespace MetricsController
{
    public class AzureConnector
    {
        public void TestSlave()
        {
            var subscriptionId = "5fa2a073-308e-4ce6-bc36-9bebd83a9b74";
            var clientId = "13698b58-36e0-47fa-b3b0-7403b299736a";
            var secret = "732e0bcb-fa05-490b-a1ee-61ef30122d4e";
            var tenantId = "4fbc361f-19e2-4f45-bbd2-94d800c170eb";
            //var queryString = "name.value eq 'Disk Write Operations/Sec' or  name.value eq 'Percentage CPU' or  name.value eq 'Network In' or  name.value eq 'Network Out' or  name.value eq 'Disk Read Operations/Sec' or  name.value eq 'Disk Read Bytes' or  name.value eq 'Disk Write Bytes'";
            //var queryString = "apiName eq 'PutBlob' and responseType eq 'Success' and geoType eq 'Primary'";
            var metrics = new[] { "Percentage CPU", "Disk Write Operations/Sec", "Disk Read Operations/Sec", "Disk Read Bytes", "Disk Write Bytes" };
            Microsoft.Azure.Management.Fluent.IAzure azure = AuthenticateWithMonitoringClient(tenantId, clientId, secret, subscriptionId).Result;
            var vms = azure.VirtualMachines.ListByResourceGroup("HBONPREMTEST");
            foreach (var vm in vms.Where(item => item.Name.Contains("Slave")))
            {
                var metricDefinitions = azure.MetricDefinitions.ListByResource(vm.Id)
                    .Where(item => metrics.Any(i => i == item.Name.Value));
                foreach (var metricDefinition in metricDefinitions)
                {
                    DateTime recordDateTime = DateTime.UtcNow;
                    var metricCollection = metricDefinition.DefineQuery()
                    .StartingFrom(recordDateTime.AddMinutes(-15))
                    .EndsBefore(recordDateTime)
                    .WithAggregation("Average")
                    .WithInterval(TimeSpan.FromMinutes(15))
                    .Execute();
                }
            }           
        }
            private static async Task<Microsoft.Azure.Management.Fluent.IAzure> AuthenticateWithMonitoringClient(string tenantId, string clientId, string secret, string subscriptionId)
            {
                var credentials = SdkContext.AzureCredentialsFactory
                                .FromServicePrincipal(clientId,
                                secret,
                                tenantId,
                                AzureEnvironment.AzureGlobalCloud);
                var azure = Microsoft.Azure.Management.Fluent.Azure
                    .Configure()
                    .Authenticate(credentials)
                    .WithDefaultSubscription();
                return azure;
            }
        }
    }

