using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using IntegrationAPITestsService.Models;
using Microsoft.Extensions.Configuration;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;

namespace IntegrationAPITestsService.Publishers
{
    public class LogsPublisher
    {
        //private readonly List<Sender> _senders = new List<Sender>(1);
        //private readonly NLog.ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly INotificationPublisher _publisher;
        
        public LogsPublisher(HbApiTesterSettings settings, IConfiguration configuration, IServiceProvider serviceProvider, INotificationPublisher publisher)
        {
           // _logger = logger;
            _configuration = configuration;
            _publisher = publisher;
            // Helper.FetchSenders(_logger, settings, _senders, serviceProvider);
        }

        public void PublishLogs(string logsText) 
            => SendTextMessage(logsText);
        
        
        private void SendTextMessage(string text)
        {
            var message = new MessengerMessageRun
            {
                logText = text,
                ChannelName = "IntegrationTester"
            };
            _publisher.Publish(message);
        }
    }
}