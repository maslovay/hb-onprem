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

        public DialogueStatusCheckerJob(IServiceProvider provider)
        {
            _repository = provider.GetRequiredService<IGenericRepository>();
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var dialogues = await _repository.FindByConditionAsync<Dialogue>(item => item.StatusId == 1);
            if (!dialogues.Any())
            {
                return;
            }
            foreach (var dialogue in dialogues)
            {
                var dialogueFrame = _repository
                    .Get<DialogueFrame>().Any(item => item.DialogueId == dialogue.DialogueId);
                var dialogueAudio = _repository
                    .Get<DialogueFrame>().Any(item => item.DialogueId == dialogue.DialogueId);
                var dialogueInterval = _repository
                    .Get<DialogueInterval>().Any(item => item.DialogueId == dialogue.DialogueId);
                var dialogueVisual = _repository
                    .Get<DialogueVisual>().Any(item => item.DialogueId == dialogue.DialogueId);
                var dialogueClientProfiles = _repository
                    .Get<DialogueClientProfile>().Any(item => item.DialogueId == dialogue.DialogueId);

                if (dialogueFrame && dialogueAudio && dialogueInterval && dialogueVisual &&
                    dialogueClientProfiles)
                {
                    dialogue.StatusId = 2;
                    _repository.Update(dialogue);
                }
                else
                {
                    if ((DateTime.Now - dialogue.CreationTime).Hours >= 2)
                    {
                        dialogue.StatusId = 8;
                        _repository.Update(dialogue);
                    }
                }
            }
            _repository.Save();
        }
    }
}
