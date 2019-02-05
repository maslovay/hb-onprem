using System;
using System.Collections.Generic;
using System.Linq;
using HBData.Models;
using HBLib.Extensions;
using HBLib.Models;
using HBLib.Utils;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace OperationService.Legacy
{
    public static class FillingSubText
    {
        [FunctionName("Filling_Sub_Text")]
        public static async System.Threading.Tasks.Task RunAsync(
            string msg,
            ExecutionContext dir,
            ILogger log)
        {
            // { "DialogueId": <dialogueId> }
            dynamic msgJs = JsonConvert.DeserializeObject(msg);
            string dialogueId, blobContainerName;

            try
            {
                dialogueId = msgJs["DialogueId"];
                blobContainerName = msgJs["BlobContainerName"];
            }
            catch
            {
                log.LogError("Failed to read message");
                throw;
            }

            var dialogue = new Dialogue();
            try
            {
                dialogue = HeedbookMessengerStatic.Context().Dialogues.First();
            }
            catch
            {
                log.LogError($"No such dialogue in sql database {dialogueId}");
                throw;
            }

            var begTime = dialogue.BegTime;
            var endTime = dialogue.EndTime;

            try
            {
                var collection =
                    HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(EnvVar.Get("CollectionAudioSTTGoogle"));

                // make request to get the documents
                var mask = new BsonDocument {{"DialogueId", dialogueId}, {"BlobContainerName", blobContainerName}};
                var docs = collection.Find(mask).ToList();

                if (docs.Count == 0)
                {
                    log.LogError($"No records found for audio.stt {dialogueId}");
                    return;
                }

                var doc = docs[0];

                string NumPhrase(int Value)
                {
                    if (Value == 0) return "Ноль";
                    string[] Dek1 =
                    {
                        "", " один", " два", " три", " четыре", " пять", " шесть", " семь", " восемь", " девять",
                        " десять", " одиннадцать", " двенадцать", " тринадцать", " четырнадцать", " пятнадцать",
                        " шестнадцать", " семнадцать", " восемнадцать", " девятнадцать"
                    };
                    string[] Dek2 =
                    {
                        "", "", " двадцать", " тридцать", " сорок", " пятьдесят", " шестьдесят", " семьдесят",
                        " восемьдесят", " девяносто"
                    };
                    string[] Dek3 =
                    {
                        "", " сто", " двести", " триста", " четыреста", " пятьсот", " шестьсот", " семьсот",
                        " восемьсот", " девятьсот"
                    };
                    string[] Th =
                        {"", "", " тысяч", " миллион", " миллиард", " триллион", " квадрилион", " квинтилион"};
                    string str = "";
                    for (byte th = 1; Value > 0; th++)
                    {
                        int gr = Value % 1000;
                        Value = (Value - gr) / 1000;
                        if (gr > 0)
                        {
                            byte d3 = (byte) ((gr - gr % 100) / 100);
                            byte d1 = (byte) (gr % 10);
                            byte d2 = (byte) ((gr - d3 * 100 - d1) / 10);
                            if (d2 == 1) d1 += (byte) 10;
                            str = Dek3[d3] + Dek2[d2] + Dek1[d1] + Th[th] + str;
                        }

                        ;
                    }

                    ;
                    return str;
                }

                int Transcription_word(string str)
                {
                    var vowels = new List<string>(new string[] {"а", "у", "о", "ы", "и", "э", "я", "ю", "ё", "е"});
                    var special_vowels = new List<string>(new string[] {"я", "ё", "е", "ю"});
                    var special_sign = new List<string>(new string[] {"ъ", "ь"});
                    var consonant = new List<string>(new string[]
                    {
                        "б", "в", "г", "д", "й", "ж", "з", "к", "л", "м", "н", "п", "р", "с", "т", "ф", "х", "ц", "ч",
                        "ш", "щ"
                    });
                    var result = 0;
                    for (int i = 0; i < str.Length; i++)
                    {
                        if (vowels.Contains(str[i].ToString()) == true ||
                            consonant.Contains(str[i].ToString()) == true ||
                            special_sign.Contains(str[i].ToString()) == true)
                        {
                            if (i == 0)
                            {
                                if (special_vowels.Contains(str[i].ToString()) == true)
                                {
                                    result += 2;
                                }
                                else
                                {
                                    result += 1;
                                }
                            }
                            else
                            {
                                if (special_sign.Contains(str[i].ToString()) == true)
                                {
                                    result += 0;
                                }
                                else
                                {
                                    if (special_vowels.Contains(str[i].ToString()) == true)
                                    {
                                        if (vowels.Contains(str[i - 1].ToString()) == true ||
                                            special_sign.Contains(str[i - 1].ToString()) == true)
                                        {
                                            result += 2;
                                        }
                                        else
                                        {
                                            result += 1;
                                        }
                                    }
                                    else
                                    {
                                        if (str[i].ToString() == "ч")
                                        {
                                            if (str[i - 1].ToString() == "c")
                                            {
                                                result += 0;
                                            }
                                            else
                                            {
                                                result += 1;
                                            }
                                        }
                                        else
                                        {
                                            if (str[i].ToString() == "c")
                                            {
                                                if (str[i - 1].ToString() == "т")
                                                {
                                                    result += 0;
                                                }
                                                else
                                                {
                                                    result += 1;
                                                }
                                            }
                                            else
                                            {
                                                result += 1;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            result = str.Length;
                            break;
                        }
                    }

                    int number;
                    bool str_number = Int32.TryParse(str, out number);
                    if (number != 0)
                    {
                        string str_str = NumPhrase(number);
                        result = Transcription(str_str);
                    }

                    return result;
                }

                int Transcription(string str)
                {
                    int result = 0;
                    string[] separators = {",", ".", "!", "?", ";", ":", " "};
                    string[] words = str.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < words.Length; i++)
                    {
                        result += Transcription_word(words[i]);
                    }

                    return result;
                }

                var fullText = "";
                var allWords = new List<BsonValue>();

                try
                {
                    allWords = BsonSerializer.Deserialize<BsonArray>(doc["Value"]["Words"].ToJson()).Distinct()
                                             .ToList();
                }
                catch
                {
                }

                var length = allWords.Count();
                bool IsClient = doc["IsClient"].ToBoolean();
                double wordsDuration = 0;
                double pauseDuration = 0;
                int sounds = 0;
                if (length == 0)
                {
                    DateTime localDate = DateTime.Now;
                    RecordDialogueSpeech(Guid.NewGuid(), dialogueId, null, null, null, false);
                    RecordDialogueWord(Guid.NewGuid(), dialogueId, localDate, localDate, null, IsClient);
                    return;
                }

                //var deleteWords = HeedbookMessengerStatic.context.DialogueWords.Where(p => p.DialogueId.ToString() == dialogueId).ToList();
                //if (deleteWords.Count() > 0)
                //{
                //    log.Info($"Deleting words {deleteWords.Count()}");
                //    HeedbookMessengerStatic.context.DialogueWords.RemoveRange(deleteWords);
                //    HeedbookMessengerStatic.context.SaveChanges();
                //}

                for (int j = 0; j < length; j++)
                {
                    var textData = allWords[j];
                    fullText += textData["Word"].ToString() + " ";
                    wordsDuration +=
                        Convert.ToDouble((Convert.ToDateTime(textData["EndTime"]) -
                                          Convert.ToDateTime(textData["BegTime"])).TotalSeconds);
                    sounds += Transcription(textData["Word"].ToString());
                    bool isClient;
                    isClient = IsClient;
                    //try
                    //{
                    //    isClient = Convert.ToInt32(textData["SpeakerTag"]) != 1 ? true : false;
                    //}
                    //catch (Exception e)
                    //{
                    //    //log.Info($"Speaker tag doesn't exist in mongo document {e}");
                    //    isClient = IsClient;
                    //}

                    pauseDuration += (j > 0)
                        ? Convert.ToDouble((Convert.ToDateTime(textData["BegTime"]) -
                                            Convert.ToDateTime(allWords[j - 1]["EndTime"])).TotalSeconds)
                        : 0;
                    try
                    {
                        RecordDialogueWord(Guid.NewGuid(), dialogueId,
                            Convert.ToDateTime(textData["BegTime"]), Convert.ToDateTime(textData["EndTime"]),
                            Convert.ToString(textData["Word"]), isClient);
                    }
                    catch (Exception e)
                    {
                        log.LogError($"No records found {e}");
                    }
                }

                try
                {
                    log.LogInformation(
                        $"Total words duration ---- {wordsDuration}, Total pause duration ----- {pauseDuration}, Total sounds ------- {sounds}");
                    var silenceShare = (endTime.Subtract(begTime).TotalSeconds > 0)
                        ? 100 * Math.Max(endTime.Subtract(begTime).TotalSeconds - wordsDuration, 0.01) /
                          endTime.Subtract(begTime).TotalSeconds
                        : 0;
                    var speechSpeed = wordsDuration != 0
                        ? Convert.ToDouble(sounds) / Convert.ToDouble(wordsDuration)
                        : 0;

                    double? sentimentScore;
                    try
                    {
                        log.LogInformation($"Text {fullText}");
                        sentimentScore = PositiveShare(fullText);
                    }
                    catch (Exception e)
                    {
                        log.LogError($"Sentiment can't be determined {e}");
                        sentimentScore = null;
                    }

                    RecordDialogueSpeech(Guid.NewGuid(), dialogueId, silenceShare, speechSpeed, sentimentScore,
                        IsClient);
                }
                catch (Exception e)
                {
                    log.LogError($"No records found while filling the text {e}");
                }


                void RecordDialogueWord(Guid DialogueWordId, string DialogueId, DateTime BegTime, DateTime EndTime,
                    string Word, bool IsClients)
                {
                    var emp = new DialogueWord();
                    emp.DialogueWordId = DialogueWordId;
                    emp.DialogueId = Guid.Parse(DialogueId);
                    emp.BegTime = BegTime;
                    emp.EndTime = EndTime;
                    emp.Word = Word;
                    emp.IsClient = IsClients;
                    HeedbookMessengerStatic.Context().DialogueWords.Add(emp);
                    HeedbookMessengerStatic.Context().SaveChanges();
                }

                void RecordDialogueSpeech(Guid DialogueSpeechId, string DialogueId, double? SilenceShare,
                    double? SpeechSpeed, double? PositiveShare, bool Client)
                {
                    var emp = new DialogueSpeech();
                    emp.IsClient = Client;
                    emp.DialogueSpeechId = DialogueSpeechId;
                    emp.DialogueId = Guid.Parse(DialogueId);
                    emp.SilenceShare = SilenceShare;
                    emp.SpeechSpeed = SpeechSpeed;
                    emp.PositiveShare = PositiveShare;
                    HeedbookMessengerStatic.Context().DialogueSpeeches.Add(emp);
                    HeedbookMessengerStatic.Context().SaveChanges();
                }

                var publishJs = new Dictionary<string, string> {{"DialogueId", dialogueId}};
                HeedbookMessengerStatic.ServiceBusMessenger.Publish(EnvVar.Get("TopicPhraseCreation"),
                    publishJs.JsonPrint());

                log.LogInformation($"Function finished: {dir.FunctionName}");
            }
            catch (Exception e)
            {
                log.LogError($"Exception occured {e}");
                throw;
            }
        }

        public static double? PositiveShare(string text)
        {
            // Create a client.

            ITextAnalyticsAPI client = new TextAnalyticsAPI();
            client.AzureRegion = AzureRegions.Westeurope;
            client.SubscriptionKey = EnvVar.Get("TextAnalytics1");
            LanguageBatchResult language = client.DetectLanguage(
                new BatchInput(
                    new List<Input>()
                    {
                        new Input("1", text)
                    }));
            var textLanguage = language.Documents[0].DetectedLanguages[0].Iso6391Name;
            var result = client.Sentiment(
                new MultiLanguageBatchInput(
                    new List<MultiLanguageInput>()
                    {
                        new MultiLanguageInput(textLanguage, "0", text),
                    }));

            return result.Documents[0].Score;
        }
    }
}