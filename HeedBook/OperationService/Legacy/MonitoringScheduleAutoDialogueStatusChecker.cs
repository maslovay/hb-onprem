using System;
using System.Collections.Generic;
using System.Linq;
using HBLib.Extensions;
using HBLib.Utils;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace OperationService.Legacy
{
    public static class MonitoringScheduleAutoDialogueStatusChecker
    {
        [FunctionName("Monitoring_Schedule_AutoDialogueStatusChecker")]
        public static void Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer,
            ILogger log,
            ExecutionContext dir)
        {
            var messenger = new HeedbookMessenger();
            try
            {
                var fillingDelaySeconds = 300 * 3;
                var mandatoryTableNames = new List<string>
                {
                    "DialogueClientProfile",
                    "DialogueAudio",
                    "DialogueFrame",
                    "DialogueInterval",
                    "DialogueVisual",
                };


                var dialogues = HeedbookMessengerStatic.Context().Dialogues.Where(p => p.StatusId == 11).ToList();
                log.LogInformation($"Dialogues count {dialogues.Count()}");

                if (dialogues.Count == 0)
                {
                    log.LogInformation("No dialogues to process");
                }

                foreach (var dialogue in dialogues)
                {
                    var isUnfilled = false;
                    var unfilledTables = new List<string>();
                    var tableCounts = new List<int>();

                    var comment = "";

                    tableCounts.Add(HeedbookMessengerStatic
                                   .Context().DialogueClientProfiles.Where(p => p.DialogueId == dialogue.DialogueId)
                                   .ToList().Count);
                    tableCounts.Add(HeedbookMessengerStatic
                                   .Context().DialogueAudios.Where(p => p.DialogueId == dialogue.DialogueId).ToList()
                                   .Count);
                    tableCounts.Add(HeedbookMessengerStatic
                                   .Context().DialogueFrames.Where(p => p.DialogueId == dialogue.DialogueId).ToList()
                                   .Count);
                    tableCounts.Add(HeedbookMessengerStatic
                                   .Context().DialogueIntervals.Where(p => p.DialogueId == dialogue.DialogueId).ToList()
                                   .Count);
                    tableCounts.Add(HeedbookMessengerStatic
                                   .Context().DialogueVisuals.Where(p => p.DialogueId == dialogue.DialogueId).ToList()
                                   .Count);


                    for (int i = 0; i < tableCounts.Count; i++)
                    {
                        if (tableCounts[i] == 0)
                        {
                            isUnfilled = true;
                            unfilledTables.Add(mandatoryTableNames[i]);
                            comment += mandatoryTableNames[i] + " is unfilled, ";
                        }
                    }

                    if (!isUnfilled)
                    {
                        var intersectDialogue = HeedbookMessengerStatic.Context().Dialogues.Where(p =>
                            p.ApplicationUserId ==
                            dialogue.ApplicationUserId &&
                            p.StatusId == 12 &&
                            p.BegTime == dialogue.BegTime &&
                            p.EndTime == dialogue.EndTime).ToList();

                        if (intersectDialogue.Count() == 0)
                        {
                            // Active
                            dialogue.StatusId = 12;
                            dialogue.Comment = "";
                            dialogue.InStatistic = true;
                            log.LogInformation($"Dialogue {dialogue.DialogueId.ToString()} is filled successfully");

                            var publishJs = new Dictionary<string, string>
                                {{"DialogueId", dialogue.DialogueId.ToString()}};

                            HeedbookMessengerStatic.ServiceBusMessenger.Publish(EnvVar.Get("TopicDialogueFilling"),
                                publishJs.JsonPrint());
                            HeedbookMessengerStatic.Context().SaveChanges();
                        }
                        else
                        {
                            Clear(dialogue.DialogueId, log);
                            log.LogInformation(
                                $"Dialogue {dialogue.DialogueId.ToString()} is filled successfully but already exist");
                        }
                    }
                    else if (dialogue.CreationTime < DateTime.Now.AddSeconds(-fillingDelaySeconds))
                    {
                        // Error
                        if (dialogue.Comment != "Retry")
                        {
                            if (dialogue.Comment == "DialogueAudio is unfilled, DialogueInterval is unfilled, " &
                                HeedbookMessengerStatic.BlobStorageMessenger.Exist("dialoguevideos",
                                    dialogue.DialogueId.ToString() + ".mkv") == false &
                                (dialogue.BegTime > DateTime.Now.AddMinutes(-2880)))
                            {
                                log.LogInformation($"Remerging dialogue {dialogue.DialogueId}");
                                RemergeDialogue(dialogue.DialogueId.ToString(), messenger, log);
                            }
                            else if (dialogue.Comment == "DialogueAudio is unfilled, DialogueInterval is unfilled, " &
                                     HeedbookMessengerStatic.BlobStorageMessenger.Exist("dialogueaudios",
                                         dialogue.DialogueId.ToString() + ".wav") == false &
                                     (dialogue.BegTime > DateTime.Now.AddMinutes(-2880)))
                            {
                                log.LogInformation(
                                    $"Try to collect audio one more time in dialogue {dialogue.DialogueId}");
                                dialogue.Comment = "Retry";
                                dialogue.CreationTime = DateTime.Now;
                                dialogue.StatusId = null;
                                HeedbookMessengerStatic.Context().SaveChanges();

                                var clientServiceBus = messenger.serviceBus;
                                clientServiceBus.Publish("blob-dialoguevideos",
                                    $"dialoguevideos/{dialogue.DialogueId.ToString()}.mkv");
                            }
                            else
                            {
                                dialogue.StatusId = 13;
                                if (dialogue.Comment == "" | dialogue.Comment == null | dialogue.Comment == "None")
                                {
                                    dialogue.Comment = comment;
                                }

                                dialogue.InStatistic = true;
                                HeedbookMessengerStatic.Context().SaveChanges();
                            }
                        }
                        else
                        {
                            dialogue.StatusId = 13;
                            if (dialogue.Comment == "" | dialogue.Comment == null | dialogue.Comment == "None")
                            {
                                dialogue.Comment = comment;
                            }

                            dialogue.InStatistic = true;
                            HeedbookMessengerStatic.Context().SaveChanges();
                            log.LogError(
                                $"Dialogue {dialogue.DialogueId.ToString()} is unfilled. Unfilled tables: {unfilledTables.JsonPrint()}");
                        }
                    }
                    else
                    {
                        continue;
                    }
                }

                HeedbookMessengerStatic.Context().SaveChanges();
                log.LogInformation($"Function finished: {dir.FunctionName}");
            }
            catch (Exception e)
            {
                log.LogError($"Exception occured {e}");
                throw;
            }
        }

        public static void RemergeDialogue(string dialogueId, HeedbookMessenger messenger, ILogger log)
        {
            var dialogue = HeedbookMessengerStatic
                          .Context().Dialogues.First(p => p.DialogueId.ToString() == dialogueId);

            if (dialogue.Comment == "DialogueAudio is unfilled, DialogueInterval is unfilled, " &
                HeedbookMessengerStatic.BlobStorageMessenger.Exist("dialoguevideos", dialogueId + ".mkv") == false)
            {
                dialogue.Comment = "Retry";
                dialogue.StatusId = null;
                dialogue.CreationTime = DateTime.Now;
                HeedbookMessengerStatic.Context().SaveChanges();

                Clear(Guid.Parse(dialogueId), log);

                var sb = new MessageStructure();
                sb.DialogueId = Guid.Parse(dialogueId);
                sb.ApplicationUserId = dialogue.ApplicationUserId;
                sb.LanguageId = dialogue.LanguageId;
                sb.BegTime = dialogue.BegTime.ToString("yyyyMMddHHmmss");
                sb.EndTime = dialogue.EndTime.ToString("yyyyMMddHHmmss");


                var clientServiceBus = messenger.serviceBus;
                clientServiceBus.Publish(EnvVar.Get("TopicDialogueCreation"), JsonConvert.SerializeObject(sb));
            }

            HeedbookMessengerStatic.Context().SaveChanges();
        }

        public class MessageStructure
        {
            public Guid DialogueId { get; set; }
            public string ApplicationUserId { get; set; }
            public string BegTime { get; set; }
            public string EndTime { get; set; }
            public int? LanguageId { get; set; }
        }

        public static void Clear(Guid dialogueId, ILogger log)
        {
            try
            {
                var dialogueAudios = HeedbookMessengerStatic
                                    .Context().DialogueAudios.Where(p => p.DialogueId == dialogueId).ToList();
                HeedbookMessengerStatic.Context().DialogueAudios.RemoveRange(dialogueAudios);
            }
            catch
            {
                log.LogInformation("Dialogue Audio is empty");
            }

            try
            {
                var dialogueClientProfiles = HeedbookMessengerStatic
                                            .Context().DialogueClientProfiles.Where(p => p.DialogueId == dialogueId)
                                            .ToList();
                HeedbookMessengerStatic.Context().DialogueClientProfiles.RemoveRange(dialogueClientProfiles);
            }
            catch
            {
                log.LogInformation("Dialogue Audio is empty");
            }

            try
            {
                var tmp = HeedbookMessengerStatic
                         .Context().DialogueClientSatisfactions.Where(p => p.DialogueId == dialogueId).ToList();
                HeedbookMessengerStatic.Context().DialogueClientSatisfactions.RemoveRange(tmp);
            }
            catch
            {
                log.LogInformation("Dialogue Audio is empty");
            }

            try
            {
                var tmp = HeedbookMessengerStatic
                         .Context().DialogueFrames.Where(p => p.DialogueId == dialogueId).ToList();
                HeedbookMessengerStatic.Context().DialogueFrames.RemoveRange(tmp);
            }
            catch
            {
                log.LogInformation("Dialogue Audio is empty");
            }

            try
            {
                var tmp = HeedbookMessengerStatic
                         .Context().DialogueIntervals.Where(p => p.DialogueId == dialogueId).ToList();
                HeedbookMessengerStatic.Context().DialogueIntervals.RemoveRange(tmp);
            }
            catch
            {
                log.LogInformation("Dialogue Audio is empty");
            }

            try
            {
                var tmp = HeedbookMessengerStatic
                         .Context().DialoguePhraseCounts.Where(p => p.DialogueId == dialogueId).ToList();
                HeedbookMessengerStatic.Context().DialoguePhraseCounts.RemoveRange(tmp);
            }
            catch
            {
                log.LogInformation("Dialogue Audio is empty");
            }

            try
            {
                var tmp = HeedbookMessengerStatic
                         .Context().DialoguePhrasePlaces.Where(p => p.DialogueId == dialogueId).ToList();
                HeedbookMessengerStatic.Context().DialoguePhrasePlaces.RemoveRange(tmp);
            }
            catch
            {
                log.LogInformation("Dialogue Audio is empty");
            }

            try
            {
                var tmp = HeedbookMessengerStatic
                         .Context().DialoguePhrases.Where(p => p.DialogueId == dialogueId).ToList();
                HeedbookMessengerStatic.Context().DialoguePhrases.RemoveRange(tmp);
            }
            catch
            {
                log.LogInformation("Dialogue Audio is empty");
            }

            try
            {
                var tmp = HeedbookMessengerStatic
                         .Context().DialogueVisuals.Where(p => p.DialogueId == dialogueId).ToList();
                HeedbookMessengerStatic.Context().DialogueVisuals.RemoveRange(tmp);
            }
            catch
            {
                log.LogInformation("Dialogue Audio is empty");
            }

            try
            {
                var tmp = HeedbookMessengerStatic
                         .Context().DialogueWords.Where(p => p.DialogueId == dialogueId).ToList();
                HeedbookMessengerStatic.Context().DialogueWords.RemoveRange(tmp);
            }
            catch
            {
                log.LogInformation("Dialogue Audio is empty");
            }
        }
    }
}