using System;
using System.Threading.Tasks;
using HBLib;
using IntegrationAPITestsService.CommandHandler;
using RabbitMqEventBus;

namespace IntegrationAPITestsService
{
    public class IntegrationTests
    {
        private readonly INotificationPublisher _publisher;
        //private readonly ElasticClientFactory _elasticClientFactory;
        private readonly CommandManager _commandManager;
        public IntegrationTests(INotificationPublisher publisher, ElasticClientFactory elasticClientFactory, CommandManager commandManager)
        {
            _publisher = publisher;
            //_elasticClientFactory = elasticClientFactory;
            _commandManager = commandManager;
        }
        public async Task Run(String command)
        {
            // var _log = _elasticClientFactory.GetElasticClient();
            // _log.SetFormat("{Path}");
            // _log.SetArgs(command);
            try
            {
                if(command != null)
                {
                    _commandManager.RunCommand(command);
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"exception: {e}");
                //_log.Fatal($"exception occured {e}");
            }
        }
    }
}