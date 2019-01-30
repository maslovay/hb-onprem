using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace OperationService.Legacy
{
    public static class FillingSubFrame
    {
        public static async System.Threading.Tasks.Task RunAsync(
            string mySbMsg,
            ILogger log)
        {
            DateTime begTime, endTime;
            string dialogueId, applicationUserId;
            int langId;
            dynamic msgJs = JsonConvert.DeserializeObject(mySbMsg);

            // PARAMETERS
            var faceYawMin = Convert.ToDouble(EnvVar.Get("FaceYawMin"));
            var faceYawMax = Convert.ToDouble(EnvVar.Get("FaceYawMax"));
            double attention = 0;
            float anger = 0, contempt = 0, disgust = 0, fear = 0, happiness = 0, neutral = 0, sadness = 0, surprise = 0;
            double? age = 0;
            var genderCount = 0;

            //PARSE MESSAGE FROM SERVICE BUS
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

            var dialogueName = $"{dialogueId}.jpg";

            try
            {
                void RecordDialogueClientProfile(string Gender, double? Age, string DialogueId, string Avatar)
                {
                    var emp = new DialogueClientProfile();
                    emp.DialogueClientProfileId = Guid.NewGuid();
                    emp.Age = Age;
                    emp.Gender = Gender;
                    emp.DialogueId = Guid.Parse(DialogueId);
                    emp.Avatar = Avatar;
                    HeedbookMessengerStatic.Context().DialogueClientProfiles.Add(emp);
                    HeedbookMessengerStatic.Context().SaveChanges();
                } 

                void RecordDialogueFrame(string DialogueId, DateTime Time, double? HappinessShare,
                    double? NeutralShare, double? SurpriseShare, double? SadnessShare, double? AngerShare,
                    double? DisgustShare, double? ContemptShare, double? FearShare, double? YawShare)
                {
                    var emp = new DialogueFrame();
                    emp.DialogueFrameId = Guid.NewGuid();
                    emp.DialogueId = Guid.Parse(DialogueId);
                    emp.Time = Time;
                    emp.AngerShare = AngerShare;
                    emp.ContemptShare = ContemptShare;
                    emp.DisgustShare = DisgustShare;
                    emp.HappinessShare = HappinessShare;
                    emp.NeutralShare = NeutralShare;
                    emp.SadnessShare = SadnessShare;
                    emp.SurpriseShare = SurpriseShare;
                    emp.FearShare = FearShare;
                    emp.YawShare = YawShare;
                    HeedbookMessengerStatic.Context().DialogueFrames.Add(emp);
                    HeedbookMessengerStatic.Context().SaveChanges();
                }

                void RecordDialogueVisual(Guid DialogueVisualId, string DialogueId, float? AttentionShare,
                    float? HappinessShare, float? NeutralShare, float? SurpriseShare, float? SadnessShare,
                    float? AngerShare, float? DisgustShare, float? ContemptShare, float? FearShare)
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
                    HeedbookMessengerStatic.Context().DialogueVisuals.Add(emp);
                    HeedbookMessengerStatic.Context().SaveChanges();
                }

                bool isDocValid(BsonDocument doc)
                {
                    try
                    {
                        var tmp = doc["Value"][0];
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }

                async System.Threading.Tasks.Task AvatarCreationAsync(List<Face> bdocs, string dialogue)
                {
                    var avatar = bdocs.OrderByDescending(p => p.Value.Max(q => q.FaceRectangle.Height))
                                      .FirstOrDefault();
                    var bAvatar = (avatar != null) ? avatar.BlobName : null;

                    var avatarAttributes = avatar.Value.OrderByDescending(p => p.FaceRectangle.Height).FirstOrDefault();

                    int faceRectangleWidth = (avatarAttributes != null)
                        ? Convert.ToInt32(avatarAttributes.FaceRectangle.Width)
                        : 0;
                    int faceRectangleHeight = (avatarAttributes != null)
                        ? Convert.ToInt32(avatarAttributes.FaceRectangle.Height)
                        : 0;
                    int faceRectangleLeft = (avatarAttributes != null)
                        ? Convert.ToInt32(avatarAttributes.FaceRectangle.Left)
                        : 0;
                    int faceRectangleTop = (avatarAttributes != null)
                        ? Convert.ToInt32(avatarAttributes.FaceRectangle.Top)
                        : 0;
                    try
                    {
                        await HeedbookMessengerStatic.BlobStorageMessenger.CreateAvatar(Convert.ToString(bAvatar),
                            dialogue, faceRectangleWidth, faceRectangleHeight,
                            faceRectangleLeft, faceRectangleTop);
                    }
                    catch
                    {
                        log.LogError($"Can't copy avatar to clientavatar container in dialogue {dialogueId}");
                    }
                }

                var collectionFrame =
                    HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(
                        EnvVar.Get("CollectionFrameFaceMicrosoft"));
                var mask = new BsonDocument
                {
                    {"Time", new BsonDocument {{"$gte", begTime}, {"$lte", endTime}}},
                    {"ApplicationUserId", applicationUserId},
                    {"Status", "Active"}
                };

                var docs = collectionFrame.Find(mask).ToList();
                var docsMaskCount = docs.Count();

                if (docsMaskCount == 0)
                {
                    log.LogError($"No records found for frameanalyzer for dialogue {dialogueId}");
                    return;
                }

                docs = docs.Where(doc => isDocValid(doc)).ToList();
                var documents = BsonSerializer.Deserialize<List<Face>>(docs.ToJson());

                // PARSE CASE NO EMOTION RECOGNIZED
                if (documents.Count() == 0)
                {
                    RecordDialogueVisual(Guid.NewGuid(), dialogueId, null, null, null, null, null, null, null, null,
                        null);
                    for (int i = 0; i < docsMaskCount; i++)
                    {
                        RecordDialogueFrame(dialogueId,
                            Convert.ToDateTime(collectionFrame.Find(mask).ToList()[i]["Time"]), null, null, null, null,
                            null, null, null, null, null);
                    }

                    RecordDialogueClientProfile(null, null, dialogueId, dialogueName);
                    HeedbookMessengerStatic.BlobStorageMessenger.CopyBlob("frames", "clientavatars",
                        collectionFrame.Find(mask).ToList()[0]["BlobName"].ToString(),
                        dialogueName);
                    return;
                }

                //CREATE AVATARS
                await AvatarCreationAsync(documents, dialogueId);

                //FILL ALL FACE API RESULTS
                foreach (var document in documents)
                {
                    var faceRectangle = document.Value[0].FaceRectangle;
                    var faceAttribute = document.Value[0].FaceAttributes;
                    var faceEmotion = faceAttribute.Emotion;

                    var yaw = Convert.ToDouble(faceAttribute.HeadPose.Yaw);

                    attention += (yaw > faceYawMin && yaw < faceYawMax) ? 100 : 0;
                    genderCount += (faceAttribute.Gender == "male") ? 1 : -1;
                    age += Convert.ToDouble(faceAttribute.Age);

                    try
                    {
                        RecordDialogueFrame(dialogueId, document.Time, Convert.ToDouble(faceEmotion.Happiness),
                            Convert.ToDouble(faceEmotion.Neutral), Convert.ToDouble(faceEmotion.Surprise),
                            Convert.ToDouble(faceEmotion.Sadness), Convert.ToDouble(faceEmotion.Anger),
                            Convert.ToDouble(faceEmotion.Disgust), Convert.ToDouble(faceEmotion.Contempt),
                            Convert.ToDouble(faceEmotion.Fear), yaw);
                    }
                    catch (Exception e)
                    {
                        log.LogError($"Exception occured while recording to DialogueFrame Datatable {e}");
                    }

                    anger += Convert.ToSingle(faceEmotion.Anger) * 100;
                    contempt += Convert.ToSingle(faceEmotion.Contempt) * 100;
                    disgust += Convert.ToSingle(faceEmotion.Disgust) * 100;
                    fear += Convert.ToSingle(faceEmotion.Fear) * 100;
                    happiness += Convert.ToSingle(faceEmotion.Happiness) * 100;
                    neutral += Convert.ToSingle(faceEmotion.Neutral) * 100;
                    sadness += Convert.ToSingle(faceEmotion.Sadness) * 100;
                    surprise += Convert.ToSingle(faceEmotion.Surprise) * 100;
                }

                var gender = (genderCount > 0) ? "male" : "female";
                age = age / documents.Count();

                try
                {
                    RecordDialogueClientProfile(gender, age, dialogueId, dialogueName);
                }
                catch (Exception e)
                {
                    log.LogError($"Exception occured while recording to DialogueClientProfile {e}");
                }

                try
                {
                    RecordDialogueVisual(Guid.NewGuid(), dialogueId, Convert.ToSingle(attention / documents.Count()),
                        happiness / documents.Count(), neutral / documents.Count(),
                        surprise / documents.Count(), sadness / documents.Count(),
                        anger / documents.Count(), disgust / documents.Count(),
                        contempt / documents.Count(), fear / documents.Count());
                }
                catch (Exception e)
                {
                    log.LogError($"Exception occured while recording to DialogueVisuals {e}");
                }

                log.LogInformation($"Function finished: {dir.FunctionName}");
            }
            catch (Exception e)
            {
                log.LogError($"Exception occured {e}");
                throw;
            }
        }

        public class MicrosoftEmotions
        {
            public double? Anger;
            public double? Contempt;
            public double? Disgust;
            public double? Fear;
            public double? Happiness;
            public double? Neutral;
            public double? Sadness;
            public double? Surprise;
        }

        public class MicrosoftHeadPose
        {
            public double? Pitch;
            public double? Roll;
            public double? Yaw;
        }

        public class MicrosoftFaceRectangle
        {
            public double? Height;
            public double? Left;
            public double? Top;
            public double Width;
        }

        public class MicrosoftFaceAttributes
        {
            public List<Dictionary<string, object>> Accessories;
            public double? Age;
            public double? Blur;
            public MicrosoftEmotions Emotion;
            public Dictionary<string, object> Exposure;
            public Dictionary<string, object> FacialHair;
            public string Gender;
            public object Glasses;
            public object Hair;
            public MicrosoftHeadPose HeadPose;
            public object Makeup;
            public object Noise;
            public object Occlusion;
            public object Smile;
        }

        public class MicrosofFace
        {
            public MicrosoftFaceAttributes FaceAttributes;
            public string FaceId;
            public object FaceLandmarks;
            public MicrosoftFaceRectangle FaceRectangle;
        }

        public class Face
        {
            public ObjectId _id;
            public string ApplicationUserId;
            public string BlobContainer;
            public string BlobName;
            public string FaceId;
            public DateTime CreationTime;
            public DateTime Time;
            public List<MicrosofFace> Value;
            public string Status;
        }
    }
}