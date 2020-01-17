using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBLib;
using HBLib.Utils;
using Microsoft.Extensions.Configuration;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using Renci.SshNet.Common;
using MessengerReporterService.Senders;
using MessengerReporterService.Utils;
using System.Collections.Generic;
using MessengerReporterService.Models;
using Newtonsoft.Json;

namespace MessengerReporterService
{
    public class MessengerReporter
    {
        private readonly ElasticClientFactory _elasticClientFactory;
        private readonly List<Sender> _senders = new List<Sender>(1);
        private readonly NLog.ILogger _logger;
        private readonly IConfiguration _configuration;

        public MessengerReporter(
            ElasticClientFactory elasticClientFactory, HbApiTesterSettings settings, IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _elasticClientFactory = elasticClientFactory;
            _configuration = configuration;
            System.Console.WriteLine($"settings: \n{JsonConvert.SerializeObject(settings)}");
            Helper.FetchSenders(_logger, settings, _senders, serviceProvider);
        }

        public async Task Run(MessengerMessageRun message)
        {
            System.Console.WriteLine($"message arrived: {JsonConvert.SerializeObject(message)}");
            var _log = _elasticClientFactory.GetElasticClient();
            // _log.SetFormat("{Path}");
            // _log.SetArgs(path);
            try
            {
                System.Console.WriteLine($"senders count: {_senders.Count}");
                foreach (var sender in _senders)
                    sender.Send(message.logText, $"{message.ChannelName}", true);
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"Exception: \n{e}");
                _log.Fatal($"exception occured {e}");
            }
        }
    }
}