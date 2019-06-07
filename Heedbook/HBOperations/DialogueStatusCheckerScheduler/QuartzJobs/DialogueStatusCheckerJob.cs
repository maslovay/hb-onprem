using System;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBData.Models;
using HBData.Repository;
using HBLib.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;

namespace QuartzExtensions.Jobs
{
    public class DialogueStatusCheckerJob : IJob
    {
        private readonly ElasticClient _log;
        private readonly INotificationPublisher _notificationPublisher;
        private readonly IGenericRepository _repository;
        private readonly RecordsContext _context;

        public DialogueStatusCheckerJob(IServiceScopeFactory factory,
            INotificationPublisher notificationPublisher,
            ElasticClient log,
            RecordsContext context)
        {
            _repository = factory.CreateScope().ServiceProvider.GetRequiredService<IGenericRepository>();
            _notificationPublisher = notificationPublisher;
            _log = log;
            _context = context;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _log.Info("Function dialogue status checker started.");
                var dialogues = _context.Dialogues
                    .Include(p => p.DialogueFrame)
                    .Include(p => p.DialogueAudio)
                    .Include(p => p.DialogueInterval)
                    .Include(p => p.DialogueVisual)
                    .Include(p => p.DialogueClientProfile)
                    .Where(item => item.StatusId == 6)
                    .ToList();

                if (!dialogues.Any())
                {
                    _log.Info("No dialogues.");
                    return;
                }

                foreach (var dialogue in dialogues)
                {
                   
                    if (dialogue.DialogueAudio.Any() &&
                        dialogue.DialogueInterval.Any() && 
                        dialogue.DialogueVisual.Any() &&
                        dialogue.DialogueClientProfile.Any() &&
                        dialogue.DialogueFrame.Any())
                    {
                        _log.Info($"Everything is Ok. Dialogue id {dialogue.DialogueId}");
                        dialogue.StatusId = 3;
                        var @event = new FillingSatisfactionRun
                        {
                            DialogueId = dialogue.DialogueId
                        };
                        _notificationPublisher.Publish(@event);
                    }
                    else
                    {
                        if ((DateTime.UtcNow - dialogue.CreationTime).Hours > 2)
                        {
                            _log.Error($"Error dialogue. Dialogue id {dialogue.DialogueId}");
                            dialogue.StatusId = 8;
                            var comment = "";
                            comment += !dialogue.DialogueAudio.Any() ? "DialogueAudio is unfilled ," : "";
                            comment += !dialogue.DialogueInterval.Any() ? "DialogueInterval is unfilled ," : "";
                            comment += !dialogue.DialogueVisual.Any() ? "DialogueVisual is unfilled ," : "";
                            comment += !dialogue.DialogueClientProfile.Any() ? "DialogueClientProfile is unfilled ," : "";
                            comment += !dialogue.DialogueFrame.Any() ? "DialogueFrame is unfilled ," : "";
                            dialogue.Comment = comment;
                        }
                        else
                        {
                            _log.Info($"Dialogue {dialogue.DialogueId} not proceeded");
                        }
                    }
                }
                _context.SaveChanges();
                _log.Info("Function  finished.");
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occured {e}");
            }
        }

        // public async Task Execute(IJobExecutionContext context)
        // {
        //     try
        //     {
        //         _log.Info("Function dialogue status checker started.");
        //         var dialogues = await _repository.FindByConditionAsync<Dialogue>(item => item.StatusId == 6);
        //         if (!dialogues.Any())
        //         {
        //             _log.Info("No dialogues.");
        //             return;
        //         }

        //         foreach (var dialogue in dialogues)
        //         {
        //             var dialogueFrame = _repository
        //                                .Get<DialogueFrame>().Any(item => item.DialogueId == dialogue.DialogueId);
        //             var dialogueAudio = _repository
        //                                .Get<DialogueAudio>().Any(item => item.DialogueId == dialogue.DialogueId);
        //             var dialogueInterval = _repository
        //                                   .Get<DialogueInterval>().Any(item => item.DialogueId == dialogue.DialogueId);
        //             var dialogueVisual = _repository
        //                                 .Get<DialogueVisual>().Any(item => item.DialogueId == dialogue.DialogueId);
        //             var dialogueClientProfiles = _repository
        //                                         .Get<DialogueClientProfile>().Any(item =>
        //                                              item.DialogueId == dialogue.DialogueId);

        //             if (dialogueFrame && dialogueAudio && dialogueInterval && dialogueVisual &&
        //                 dialogueClientProfiles)
        //             {
        //                 _log.Info($"Everything is Ok. Dialogue id {dialogue.DialogueId}");
        //                 dialogue.StatusId = 3;
        //                 _repository.Update(dialogue);
        //                 var @event = new FillingSatisfactionRun
        //                 {
        //                     DialogueId = dialogue.DialogueId
        //                 };
        //                 _notificationPublisher.Publish(@event);
        //             }
        //             else
        //             {
        //                 if ((DateTime.Now - dialogue.CreationTime).Hours < 2) continue;
        //                 _log.Error($"Error dialogue. Dialogue id {dialogue.DialogueId}");
        //                 dialogue.StatusId = 8;
        //                 _repository.Update(dialogue);
        //             }
        //         }

        //         _repository.Save();
        //         _log.Info("Function  ended.");
        //     }
        //     catch (Exception e)
        //     {
        //         _log.Fatal($"exception occurs {e}");
        //     }
        // }
    }
}