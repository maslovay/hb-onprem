using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using HBLib.Utils;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RabbitMqEventBus.Events;
using HBLib;
using HBData;
using FillSlideShowDialogueService.Exceptions;

namespace FillSlideShowDialogueService
{
    public class FillSlideShowDialogue
    {
        private readonly RecordsContext _context;
        private readonly ElasticClientFactory _elasticClientFactory;


        public FillSlideShowDialogue (
            IServiceScopeFactory factory,
            ElasticClientFactory elasticClientFactory
        )
        {
            _context = factory.CreateScope().ServiceProvider.GetRequiredService<RecordsContext>();
            _elasticClientFactory = elasticClientFactory;
        }

        public async Task Run(FillSlideShowDialogueRun message)
        {
            var _log = _elasticClientFactory.GetElasticClient();
            _log.SetFormat("{DialogueId}");
            _log.Info("Function started");
            _log.SetArgs(JsonConvert.SerializeObject(message.DialogueId));

        }
    }
}