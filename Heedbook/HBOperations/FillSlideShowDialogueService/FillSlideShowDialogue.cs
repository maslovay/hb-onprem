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
        private readonly ElasticClient _log;


        public FillSlideShowDialogue (
            IServiceScopeFactory factory,
            ElasticClient log
        )
        {
            _context = factory.CreateScope().ServiceProvider.GetRequiredService<RecordsContext>();
            _log = log;
        }

        public async Task Run(FillSlideShowDialogueRun message)
        {
            _log.SetArgs(JsonConvert.SerializeObject(message.DialogueId));
            var dialogue = _context.Dialogues.FirstOrDefault(x => x.DialogueId == message.DialogueId);
            var slideShowSessions = _context.SlideShowSessions
                .Where(x => x.BegTime >= dialogue.BegTime && x.BegTime <= dialogue.EndTime && x.DeviceId == dialogue.DeviceId).ToList();
            slideShowSessions.Select(
                x =>
                {
                    x.DialogueId = dialogue.DialogueId;
                    return x;
                }).ToList();
            _context.SaveChanges();
            _log.Info($"{slideShowSessions.Count()} slideShowSessions filled");
        }
    }
}