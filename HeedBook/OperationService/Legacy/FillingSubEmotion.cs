using System;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBLib.Utils;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace OperationService.Legacy {
    public static class FillingSubEmotion
    {
        [FunctionName("Filling_Sub_Emotion")]
        public static async Task RunAsync(
            string mySbMsg,
            ILogger log,
            ExecutionContext dir)
        {
            if (mySbMsg == "Don't sleep!")
            {
                return;
            }
            dynamic msgJs = JsonConvert.DeserializeObject(mySbMsg);
            string applicationUserId, dialogueId;
            int langId;
            DateTime begTime, endTime;
            try
            {
                begTime = DT.Parse(msgJs["BegTime"].ToString());
                endTime = DT.Parse(msgJs["EndTime"].ToString());
                dialogueId = msgJs["DialogueId"];
                applicationUserId = msgJs["ApplicationUserId"];
                langId = Convert.ToInt32(msgJs["LanguageId"].ToString());
            }
            catch
            {
                log.LogError("Failed to read message");
                throw;
            }

            try
            {
                var context = HeedbookMessengerStatic.Context();
                    void RecordDialogueVisual(Guid DialogueVisualId, string DialogueId, float? AttentionShare, float? HappinessShare, float? NeutralShare, float? SurpriseShare, float? SadnessShare, float? AngerShare, float? DisgustShare, float? ContemptShare, float? FearShare)
                    {
                        var emp = new DialogueVisual();
                        emp.DialogueVisualId = DialogueVisualId;
                        emp.DialogueId = Guid.Parse(DialogueId);
                        emp.AttentionShare = AttentionShare;
                        emp.HappinessShare = HappinessShare;
                        emp.SadnessShare = SadnessShare;
                        emp.NeutralShare = NeutralShare;
                        emp.SurpriseShare = SurpriseShare;
                        emp.DisgustShare = DisgustShare;
                        emp.ContemptShare = ContemptShare;
                        emp.FearShare = FearShare;
                        context.DialogueVisuals.Add(emp);
                        context.SaveChanges();
                    }



                    var collectionEmotion = HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(EnvVar.Get("CollectionFrameEmotionMicrosoft"));
                    var collectionFrame = HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(EnvVar.Get("CollectionFrameFaceMicrosoft"));
                    
                    var maskEmotion = new BsonDocument { { "Time", new BsonDocument { { "$gte", begTime }, { "$lte", endTime } } } ,
                                   { "ApplicationUserId", applicationUserId} ,
                                   { "Status", "Active"} };
                    
                   var docsEmotion = collectionEmotion.Find(maskEmotion).ToList();

                    var faceYawMin = Convert.ToInt32(EnvVar.Get("FaceYawMin"));
                    var faceYawMax = Convert.ToInt32(EnvVar.Get("FaceYawMax"));
                    double attention = 0;
                    float anger = 0, contempt = 0, disgust = 0, fear = 0, happiness = 0, neutral = 0, sadness = 0, surprise = 0;

                    // todo: make properly
                    bool isDocValid(BsonDocument doc)
                    {
                        try
                        {
                            // check if bsonarray is non-empty
                            var tmp = doc["Value"][0];
                            return true;
                        }
                        catch
                        {
                            return false;
                        }
                    }

                    var docsFrame = collectionFrame.Find(maskEmotion).ToList();
                    docsFrame = docsFrame.Where(doc => isDocValid(doc)).ToList();

                    for (int i = 0; i < docsFrame.Count; i++)
                    {
                        var yaw = docsFrame[i]["Value"][0]["FaceAttributes"]["HeadPose"]["Yaw"];
                        if (yaw > faceYawMin && yaw < faceYawMax)
                        {
                            attention += 100;
                        }
                    }

                    docsEmotion = docsEmotion.Where(doc => isDocValid(doc)).ToList();
                    foreach (var doc in docsEmotion)
                    {

                        var resultEmotion = doc["Value"][0]["Scores"];
                        var dialogueIntervalId = Guid.NewGuid();

                        anger += Convert.ToSingle(resultEmotion["Anger"]) * 100;
                        contempt += Convert.ToSingle(resultEmotion["Contempt"]) * 100;
                        disgust += Convert.ToSingle(resultEmotion["Disgust"]) * 100;
                        fear += Convert.ToSingle(resultEmotion["Fear"]) * 100;
                        happiness += Convert.ToSingle(resultEmotion["Happiness"]) * 100;
                        neutral += Convert.ToSingle(resultEmotion["Neutral"]) * 100;
                        sadness += Convert.ToSingle(resultEmotion["Sadness"]) * 100;
                        surprise += Convert.ToSingle(resultEmotion["Surprise"]) * 100;
                    }

                    // todo: make properly
                    if ((docsEmotion.Count == 0) || (docsFrame.Count == 0))
                    {
                        RecordDialogueVisual(Guid.NewGuid(), dialogueId, null, null, null, null, null, null, null, null, null);
                        log.LogInformation("No emotions or frames found");
                    }
                    else
                    {
                        RecordDialogueVisual(Guid.NewGuid(), dialogueId, Convert.ToSingle(attention / docsFrame.Count),
                                             happiness / docsEmotion.Count, neutral / docsEmotion.Count,
                                             surprise / docsEmotion.Count, sadness / docsEmotion.Count,
                                             anger / docsEmotion.Count, disgust / docsEmotion.Count,
                                             contempt / docsEmotion.Count, fear / docsEmotion.Count);
                    }
                log.LogInformation($"Function finished: {dir.FunctionName}");
            }
            catch (Exception e)
            {
                log.LogError("Exception occured {e}", e);
                throw;
            }
        }
    }
}
