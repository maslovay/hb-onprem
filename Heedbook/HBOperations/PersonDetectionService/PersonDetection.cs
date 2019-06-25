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
using PersonDetectionService.Exceptions;

namespace PersonDetectionService
{
    public class PersonDetection
    {
        private readonly ElasticClient _log;
        private readonly RecordsContext _context;
        private readonly ElasticClientFactory _elasticClientFactory;
        private readonly DescriptorCalculations _calc;


        public PersonDetection(
            IServiceScopeFactory factory,
            ElasticClientFactory elasticClientFactory,
            DescriptorCalculations calc
        )
        {
            _context = factory.CreateScope().ServiceProvider.GetRequiredService<RecordsContext>();
            _elasticClientFactory = elasticClientFactory;
            _calc = calc;
        }

        public async Task Run(PersonDetectionRun message)
        {
            var _log = _elasticClientFactory.GetElasticClient();
            _log.SetFormat("{ApplicationUserIds}");
            _log.SetArgs(JsonConvert.SerializeObject(message.ApplicationUserIds));
            _log.Info("Function started");
            try
            {
                var begTime = DateTime.Now.AddYears(-1);
                var dialogues = _context.Dialogues
                    .Where(p => message.ApplicationUserIds.Contains(p.ApplicationUserId))
                    .Where(p => !String.IsNullOrEmpty(p.PersonFaceDescriptor) && p.BegTime >= begTime)
                    .OrderBy(p => p.BegTime)
                    .ToList();
                
                foreach (var curDialogue in dialogues.Where(p => p.PersonId == null).ToList())
                {
                    var dialoguesProceeded = dialogues.Where(p => p.ApplicationUserId == curDialogue.ApplicationUserId && p.PersonId != null).ToList();
                    curDialogue.PersonId = FindId(curDialogue, dialoguesProceeded);
                }
                
                _context.SaveChanges();
                _log.Info("Function finished");
            }
            catch (Exception e)
            {
                _log.Info($"Exception occured {e}");
                throw new PersonDetectionException(e.Message, e);
            }
        }

        public Guid? FindId(Dialogue curDialogue, List<Dialogue> dialogues, double treshold=0.42)
        {
            if (!dialogues.Any()) return Guid.NewGuid();
            foreach (var dialogue in dialogues)
            {
                var cosResult = _calc.Cos(curDialogue.PersonFaceDescriptor, dialogue.PersonFaceDescriptor);
                System.Console.WriteLine($"Cos distnace is -- {cosResult}");
                if (cosResult > treshold) return dialogue.PersonId;
            }
            return Guid.NewGuid();

        }
    }
}