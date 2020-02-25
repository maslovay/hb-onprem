using System;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBLib;
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
        private ElasticClient _log;
        private readonly INotificationPublisher _notificationPublisher;
        private RecordsContext _context;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ElasticClientFactory _elasticClientFactory;

        public DialogueStatusCheckerJob(IServiceScopeFactory factory,
            INotificationPublisher notificationPublisher,
            ElasticClientFactory elasticClientFactory)
        {
            _notificationPublisher = notificationPublisher;
            _elasticClientFactory = elasticClientFactory;
            _scopeFactory = factory;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            System.Console.WriteLine("Function started");
            using (var scope = _scopeFactory.CreateScope())
            {
                _log = _elasticClientFactory.GetElasticClient();
                _log.Info("Audio analyze scheduler started.");
                try
                {
                    _log.Info("Function started.");
                    _context = scope.ServiceProvider.GetRequiredService<RecordsContext>();
                    var dialogues = _context.Dialogues
                        .Include(p => p.DialogueFrame)
                        .Include(p => p.DialogueAudio)
                        .Include(p => p.DialogueInterval)
                        .Include(p => p.DialogueVisual)
                        .Include(p => p.DialogueClientProfile)
                        .Include(p => p.Device)
                        .Include(p => p.Device.Company)
                        .Where(item => item.StatusId == 6)
                        .ToList();
                    System.Console.WriteLine($"{dialogues.Count()}");


                    if (!dialogues.Any())
                    {
                        _log.Info("No dialogues.");
                        return;
                    }

                    foreach (var dialogue in dialogues)
                    {

                        if ((dialogue.Device.Company.IsExtended && dialogue.DialogueAudio.Any() &&
                            dialogue.DialogueInterval.Any() &&
                            dialogue.DialogueVisual.Any() &&
                            dialogue.DialogueClientProfile.Any() &&
                            dialogue.DialogueFrame.Any()) || (!dialogue.Device.Company.IsExtended &&  dialogue.DialogueVisual.Any() &&
                            dialogue.DialogueClientProfile.Any() &&
                            dialogue.DialogueFrame.Any()))
                        {
                            _log.Info($"Everything is Ok. Dialogue id {dialogue.DialogueId}");
                            dialogue.StatusId = 3;
                            if (dialogue.Device.Company.IsExtended)
                            {
                                var @event = new FillingSatisfactionRun
                                {
                                    DialogueId = dialogue.DialogueId
                                };
                                _notificationPublisher.Publish(@event);
                            }
                        }
                        else
                        {
                            if ((DateTime.UtcNow - dialogue.CreationTime).Minutes > 30)
                            {
                                _log.Error($"Error dialogue. Dialogue id {dialogue.DialogueId}");
                                dialogue.StatusId = 8;
                                if (dialogue.Device.Company.IsExtended)
                                {
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
                                    var comment = "";
                                    comment += !dialogue.DialogueVisual.Any() ? "DialogueVisual is unfilled ," : "";
                                    comment += !dialogue.DialogueClientProfile.Any() ? "DialogueClientProfile is unfilled ," : "";
                                    comment += !dialogue.DialogueFrame.Any() ? "DialogueFrame is unfilled ," : "";
                                    dialogue.Comment = comment;
                                }
                            }
                            else
                            {
                                _log.Info($"Dialogue {dialogue.DialogueId} not proceeded");
                            }
                        }
                        _context.SaveChanges();
                    }
                    _log.Info("Function  finished.");
                }
                catch (Exception e)
                {
                    _log.Fatal($"Exception occured {e}");
                }
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