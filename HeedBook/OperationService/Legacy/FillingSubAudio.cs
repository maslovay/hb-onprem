using System;
using System.Linq;
using HBData.Models;
using HBLib.Utils;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace OperationService.Legacy
{
    public static class FillingSubAudio
    {
        [FunctionName("Filling_Sub_Audio")]
        public static async System.Threading.Tasks.Task RunAsync(
            string msg,
            ExecutionContext dir,
            ILogger log)
        {
            // { "DialogueId": <dialogueId> }
            dynamic msgJs = JsonConvert.DeserializeObject(msg);
            string dialogueId, blobContainerName;
            bool isClient;

            try
            {
                dialogueId = msgJs["DialogueId"];
                blobContainerName = msgJs["BlobContainerName"];
            }
            catch (Exception e)
            {
                log.LogError($"Failed to read message {msg}");
                throw;
            }

            if (blobContainerName == EnvVar.Get("BlobContainerDialogueAudiosEmp"))
            {
                isClient = false;
            }
            else
            {
                isClient = true;
            }

            try
            {
                var context = HeedbookMessengerStatic.Context();
                var collection =
                    HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(
                        EnvVar.Get("CollectionAudioToneanalyzer"));

                // make request to get the documents
                var mask = new BsonDocument {{"DialogueId", dialogueId}, {"IsClient", isClient}};
                var docs = collection.Find(mask).ToList();
                var old_docs = docs;

                if (docs.Count == 0)
                {
                    log.LogError($"No records found for toneanalyzer {dialogueId}");
                    return;
                }

                bool isDocValid(BsonDocument one_doc)
                {
                    try
                    {
                        // check if bsonarray is non-empty
                        var tmp = one_doc["Value"][0];
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }

                docs = docs.Where(one_doc => isDocValid(one_doc)).ToList();
                if (docs.Count == 0)
                {
                    var old_audioData = old_docs[0]["Value"][0];
                    RecordDialogueInterval(Guid.NewGuid(), dialogueId, Convert.ToDateTime(old_audioData["BegTime"]),
                        Convert.ToDateTime(old_audioData["EndTime"]), null, null, null, null, null, false);
                    RecordDialogueAudio(Guid.NewGuid(), dialogueId, null, null, null, false);
                    return;
                }

                log.LogInformation("Add not null datas");
                var doc = docs[0];
                var applicationUserId = Convert.ToString(doc["ApplicationUserId"]);

                double neutralityTone = 0;
                double positiveTone = 0;
                double negativeTone = 0;
                var length = doc["Value"].AsBsonArray.ToArray().Count();
                var failCount = 0;

                for (int j = 0; j < length; j++)
                {
                    var audioData = doc["Value"][j];

                    if (audioData["Status"] == "Fail")
                    {
                        failCount += 1;
                        log.LogInformation("Status fail when processing tone analyzer document: {doc}", doc.ToJson());
                        continue;
                    }

                    var dialogueIntervalId = Guid.NewGuid();
                    neutralityTone += Convert.ToDouble(audioData["Neutrality"]);
                    positiveTone += Convert.ToDouble(audioData["Happiness"]);
                    negativeTone += Convert.ToDouble(audioData["Sadness"]) + Convert.ToDouble(audioData["Anger"]) +
                                    Convert.ToDouble(audioData["Fear"]);
                    try
                    {
                        RecordDialogueInterval(Guid.NewGuid(), dialogueId,
                            Convert.ToDateTime(audioData["BegTime"]), Convert.ToDateTime(audioData["EndTime"]),
                            Convert.ToDouble(audioData["Neutrality"]), Convert.ToDouble(audioData["Happiness"]),
                            Convert.ToDouble(audioData["Sadness"]), Convert.ToDouble(audioData["Anger"]),
                            Convert.ToDouble(audioData["Fear"]), isClient);
                    }
                    catch (Exception e)
                    {
                        log.LogError($"Failed to record dialogue interval {e}");
                    }
                }

                if (failCount == length)
                {
                    RecordDialogueInterval(Guid.NewGuid(), dialogueId, Convert.ToDateTime(doc["Value"][0]["BegTime"]),
                        Convert.ToDateTime(doc["Value"][length - 1]["EndTime"]), null, null, null, null, null,
                        isClient);
                    RecordDialogueAudio(Guid.NewGuid(), dialogueId, null, null, null, isClient);
                    //log.Info("All status are Failed");
                    return;
                }

                neutralityTone = 100 * neutralityTone / length;
                negativeTone = 100 * negativeTone / length;
                positiveTone = 100 * positiveTone / length;

                try
                {
                    RecordDialogueAudio(Guid.NewGuid(), dialogueId, neutralityTone, positiveTone, negativeTone,
                        isClient);
                }
                catch (Exception e)
                {
                    log.LogError($"Failed to record dialogue audio {e}");
                }

                void RecordDialogueInterval(Guid DialogueIntervalId, string DialogueId, DateTime BegTime,
                    DateTime EndTime, double? NeutralityTone, double? HappinessTone, double? SadnessTone,
                    double? AngerTone, double? FearTone, bool IsClient)
                {
                    var emp = new DialogueInterval();
                    emp.DialogueIntervalId = DialogueIntervalId;
                    emp.BegTime = BegTime;
                    emp.EndTime = EndTime;
                    emp.DialogueId = Guid.Parse(DialogueId);
                    emp.NeutralityTone = NeutralityTone;
                    emp.HappinessTone = HappinessTone;
                    emp.SadnessTone = SadnessTone;
                    emp.AngerTone = AngerTone;
                    emp.FearTone = FearTone;
                    emp.IsClient = IsClient;
                    context.DialogueIntervals.Add(emp);
                    context.SaveChanges();
                }


                void RecordDialogueAudio(Guid DialogueAudioId, string DialogueId, double? NeutralityTone,
                    double? PositiveTone, double? NegativeTone, bool IsClient)
                {
                    var emp = new DialogueAudio();
                    emp.DialogueAudioId = DialogueAudioId;
                    emp.DialogueId = Guid.Parse(DialogueId);
                    emp.NeutralityTone = NeutralityTone;
                    emp.PositiveTone = PositiveTone;
                    emp.NegativeTone = NegativeTone;
                    emp.IsClient = IsClient;
                    context.DialogueAudios.Add(emp);
                    context.SaveChanges();
                }

                log.LogInformation($"Function finished: {dir.FunctionName}");
            }
            catch (Exception e)
            {
                log.LogError($"Exception occured {e}");
                throw;
            }
        }
    }
}