using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using HBLib;
using HBData;
using FillingSatisfactionService.Services;

namespace FillingSatisfactionService
{
    public class FillingSatisfaction
    {
        private readonly INotificationPublisher _notificationPublisher;
        private readonly ElasticClientFactory _elasticClientFactory;
        private readonly FillingSatisfactionServiceCalculation _fillingSatisfactionService;


        public FillingSatisfaction(IServiceScopeFactory factory,
            INotificationPublisher notificationPublisher,
            ElasticClientFactory elasticClientFactory,
            FillingSatisfactionServiceCalculation fillingSatisfactionService
            )
        {
            _fillingSatisfactionService = fillingSatisfactionService;
            _notificationPublisher = notificationPublisher;
            _elasticClientFactory = elasticClientFactory;
        }

        public async Task Run(Guid dialogueId)
        {
             var _log = _elasticClientFactory.GetElasticClient();
            _log.SetFormat("{DialogueId}");
            _log.SetArgs(dialogueId);
            try
            {
                System.Console.WriteLine("Function started");
                _log.Info("Function started.");
                _fillingSatisfactionService.DialogueSatisfactionScoreCalculate(dialogueId);
                System.Console.WriteLine("Calculation fineshed");
                var @event = new FillingHintsRun
                {
                    DialogueId = dialogueId
                };
                _notificationPublisher.Publish(@event);
                _log.Info("Function finished.");
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occured {e}.");
                throw;
            }
        }
    }
}