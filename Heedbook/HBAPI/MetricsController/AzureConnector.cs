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
        private async void TestSlave()
        {
            string vmName = "m";
            string resourceId = "" + vmName;
            var subscriptionId = "";
            var clientId = "";
            var secret = "";
            var tenantId = "";
            resourceId = resourceId.Replace("{subscriptionId}", subscriptionId);

            Microsoft.Azure.Management.Fluent.IAzure azure = AuthenticateWithMonitoringClient(tenantId, clientId, secret, subscriptionId).Result;
            var vms = azure.VirtualMachines.ListByResourceGroup("HBOnpremTest");
            foreach(var vm in vms)
            { if (vms.)
                {
                    
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

