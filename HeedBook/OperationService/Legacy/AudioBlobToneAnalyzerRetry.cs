using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using HBLib.AzureFunctions;
using HBLib.Extensions;
using HBLib.Utils;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace OperationService.Legacy
{
    public static class AudioBlobToneAnalyzerRetry
    {
        [FunctionName("Audio_Blob_ToneAnalyzerRetry")]
        public static void Run(string msg,
            ExecutionContext dir,
            ILogger log
        )
        {
            var sessionId = Misc.GenSessionId();

            var msgSplit = Regex.Split(msg, "/");
            var blobContainerName = msgSplit[0];
            var blobName = msgSplit[1];
            var name = blobName;
            var blob = HeedbookMessengerStatic.BlobStorageMessenger.GetBlob(blobContainerName, blobName);
            // load blob metadata
            blob.FetchAttributesAsync();
            var blobMetadata = blob.Metadata;
            var applicationUserId = blobMetadata["ApplicationUserId"];
            var t = blobMetadata["BegTime"];
            var dt = DT.Parse(t);
            var langId = Convert.ToInt32(blobMetadata["LanguageId"]);
            var dialogueId = Path.GetFileNameWithoutExtension(name);
            bool isClient;
            if (blobContainerName == EnvVar.Get("BlobContainerDialogueAudiosEmp"))
            {
                isClient = false;
            }
            else
            {
                isClient = true;
            }
            //var log = LoggerFactory.CreateAdapter(ilog, dir, "{ApplicationUserId} {DialogueId}", applicationUserId, dialogueId);

            try
            {
                //log.Info($"Processing audio file tone:{name}");

                //parameters
                var localDir = Misc.GenLocalDir(sessionId);

                //load the file
                using (System.IO.FileStream output = new System.IO.FileStream(Path.Combine(localDir, name), FileMode.Create))
                {
                    blob.DownloadToStreamAsync(output);
                }

                var ffmpeg = new FFMpegWrapper(Path.Combine(Misc.BinPath(dir), "ffmpeg.exe"));

                var localAudioFn = Path.Combine(localDir, name);
                //log.Info($"{localAudioFn}");

                try
                {
                    var splitSeconds = 15;
                    var metadata = ffmpeg.SplitBySeconds(localAudioFn, splitSeconds, localDir);

                    var toneDocs = new BsonArray();

                    var curDt = dt;
                    var i = 0;

                    foreach (var curMetadata in metadata)
                    {
                        var curFn = curMetadata["fn"];
                        i++;

                        BsonDocument toneDoc;

                        try
                        {
                            var res = RecognizeTone(dir, curFn);
                            toneDoc = new BsonDocument(res);
                            toneDoc["Status"] = "Success";
                        }
                        catch (Exception e)
                        {
                            //log.Info("ToneAnalyzer failed {e}", e);
                            toneDoc = new BsonDocument { { "Status", "Fail" } };
                        }

                        toneDoc["BegTime"] = dt;
                        toneDoc["EndTime"] = dt.AddSeconds(splitSeconds);
                        toneDocs.Add(toneDoc);
                        dt = dt.AddSeconds(splitSeconds);
                    }

                    var doc = new BsonDocument { { "Value", toneDocs } };

                    var collection = HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(EnvVar.Get("CollectionAudioToneanalyzer"));

                    var now = DateTime.Now;

                    doc["ApplicationUserId"] = applicationUserId;
                    doc["Time"] = dt;
                    doc["CreationTime"] = now;
                    doc["BlobName"] = name;
                    doc["DialogueId"] = dialogueId;
                    doc["Status"] = "Active";
                    doc["IsClient"] = isClient;

                    // insert safely
                    HeedbookMessengerStatic.MongoDBMessenger.SafeReplaceOne(collection,
                                                     new BsonDocument {
                                                     { "ApplicationUserId", applicationUserId},
                                                     { "DialogueId", dialogueId},
                                                     { "IsClient", isClient}
                                                     },
                                                     doc);

                    //log.Info("Added document to mongodb database: {doc}", doc.ToJson());

                    var js = new Dictionary<string, string>();
                    js["DialogueId"] = dialogueId;
                    js["BlobContainerName"] = blobContainerName;

                    //var js = new Dictionary<string, string> { { "DialogueId", dialogueId } };
                    HeedbookMessengerStatic.ServiceBusMessenger.Publish(EnvVar.Get("TopicAudioToneanalyzer"), js.JsonPrint());

                    OS.SafeDelete(localAudioFn);
                    OS.SafeDelete(localDir);
                }
                catch (Exception e)
                {
                    OS.SafeDelete(localAudioFn);
                    OS.SafeDelete(localDir);
                    throw;
                }
                //log.Info($"Function finished: {dir.FunctionName}");
            }
            catch (Exception e)
            {
                //log.Critical("Exception occured {e}", e);
                throw;
            }
        }

        public static Dictionary<string, double> RecognizeTone(ExecutionContext dir, string fn)
        {
            /***********
            WAV files analyzed with:
            OpenVokaturi version 2.1 for open-source projects, 2017-01-13
            Distributed under the GNU General Public License, version 3 or later
            **********

            Emotion analysis of WAV file .\sample.wav:
            Neutrality 0.614982
            Happiness 0.000023
            Sadness 0.174298
            Anger 0.001639
            Fear 0.209058*/
            var cmd = new CMDWithOutput();
            var text = cmd.runCMD(Path.Combine(Misc.BinPath(dir), "MeasureWav_win64.exe"), fn);

            try
            {
                var pattern = @"\n\s?(\w+)\s+([\d\.]+)";

                /*{
                  "Neutrality": 0.614982,
                  "Happiness": 2.3E-05,
                  "Sadness": 0.174298,
                  "Anger": 0.001639,
                  "Fear": 0.209058
                }*/
                var res = new Dictionary<string, double>();

                var matches = Regex.Matches(text, pattern);
                var x = matches[0];
                foreach (Match m in matches)
                {
                    var emotion = m.Groups[1].ToString();
                    var value = m.Groups[2].ToString();
                    res[emotion] = double.Parse(value, CultureInfo.InvariantCulture.NumberFormat);
                }
                return res;
            }
            catch
            {
                throw new Exception($"Something went wrong! The error message: {text}");
            }
        }
    }
}