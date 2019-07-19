using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HBLib;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace MetricsController.QuartzJob
{
    public class GetMetricsJob : IJob
    {
        private AzureConnector _connector;
        private IServiceScopeFactory _scopeFactory;
        private readonly ElasticClientFactory _elasticClientFactory;
        public GetMetricsJob(IServiceScopeFactory scopeFactory, ElasticClientFactory elasticClientFactory)
        {
            _scopeFactory = scopeFactory;
            _elasticClientFactory = elasticClientFactory;
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
                        var metrics = _connector.GetMetrics();
                        _connector._log = _log;
                        
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
