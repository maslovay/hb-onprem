using HBData.Models;
using HBData.Repository;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using System;
using System.Linq;
using System.Threading.Tasks;
using DialogueVisual = HBData.Models.DialogueVisual;

namespace QuartzExtensions.Jobs
{
    public class DialogueStatusCheckerJob : IJob
    {
        private readonly IGenericRepository _repository;

        public DialogueStatusCheckerJob(IServiceScopeFactory scopeFactory)
        {
            var scope = scopeFactory.CreateScope();
            _repository = scope.ServiceProvider.GetRequiredService<IGenericRepository>();
        }

        public async Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("Function started.");
            var dialogues = await _repository.FindByConditionAsync<Dialogue>(item => item.StatusId == 6);
            if (!dialogues.Any())
            {
                Console.WriteLine("No dialogues.");
                return;
            }
            foreach (var dialogue in dialogues)
            {
                var dialogueFrame = _repository
                    .Get<DialogueFrame>().Any(item => item.DialogueId == dialogue.DialogueId);
                var dialogueAudio = _repository
                    .Get<DialogueAudio>().Any(item => item.DialogueId == dialogue.DialogueId);
                var dialogueInterval = _repository
                    .Get<DialogueInterval>().Any(item => item.DialogueId == dialogue.DialogueId);
                var dialogueVisual = _repository
                    .Get<DialogueVisual>().Any(item => item.DialogueId == dialogue.DialogueId);
                var dialogueClientProfiles = _repository
                    .Get<DialogueClientProfile>().Any(item => item.DialogueId == dialogue.DialogueId);

                if (dialogueFrame && dialogueAudio && dialogueInterval && dialogueVisual &&
                    dialogueClientProfiles)
                {
                    Console.WriteLine("Everything is Ok");
                    dialogue.StatusId = 7;
                    _repository.Update(dialogue);
                }
                else
                {
                    if ((DateTime.Now - dialogue.CreationTime).Hours >= 2)
                    {
                        Console.WriteLine("Error");
                        dialogue.StatusId = 8;
                        _repository.Update(dialogue);
                    }
                }
            }
            _repository.Save();
            Console.WriteLine("Function ended.");
        }
    }
}
