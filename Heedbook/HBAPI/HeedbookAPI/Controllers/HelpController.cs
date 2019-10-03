using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.Extensions.Configuration;
using HBData.Models;
using UserOperations.Services;
using HBData;
using Newtonsoft.Json;
using HBLib.Utils;
using UserOperations.Utils;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Globalization;
using HBLib;
using RabbitMqEventBus.Events;
using Notifications.Base;
using HBMLHttpClient;
using Renci.SshNet.Common;
using UserOperations.Models.AnalyticModels;
using HBMLHttpClient.Model;
using System.Drawing;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HelpController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
        private readonly SftpClient _sftpClient;
        private readonly MailSender _mailSender;
        private readonly RequestFilters _requestFilters;
        private readonly SftpSettings _sftpSettings;
        private readonly ElasticClient _log;
        private readonly DBOperations _dbOperation;
        //   private readonly INotificationHandler _handler;
        //    private readonly HbMlHttpClient _client;

        private readonly Object _syncRoot = new Object();


        public HelpController(
            IConfiguration config,
            ILoginService loginService,
            RecordsContext context,
            SftpClient sftpClient,
            MailSender mailSender,
            RequestFilters requestFilters,
            SftpSettings sftpSettings,
            ElasticClient log,
            DBOperations dBOperations
            //     INotificationHandler handler,
            //     HbMlHttpClient client
            )
        {
            _config = config;
            _loginService = loginService;
            _context = context;
            _sftpClient = sftpClient;
            _mailSender = mailSender;
            _requestFilters = requestFilters;
            _sftpSettings = sftpSettings;
            _log = log;
            _dbOperation = dBOperations;
            //   _handler = handler;
            //   _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        [HttpGet("SendToAvatarMake")]
        public async Task<IActionResult> SendToAvatarMake(int start)
        {
            string html = string.Empty;
            int counter200 = 0;
            int counter500 = 0;

            var users = _context.ApplicationUsers.Skip(start).Take(100).Select(x => x.Id).ToList();
            foreach (var item in users)
            {
                string url = @"https://heedbookslave.northeurope.cloudapp.azure.com/api/Help/ClientAvatarMaker?ApplicationUserId=" + item;

                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                request.Method = "GET";
                String test = String.Empty;
                try {
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        counter200++;
                    }
                }
                catch
                {
                    counter500++;
                }
                }
            return Ok(html);
        }


        [HttpGet("ClientAvatarMaker")]
        public async Task<IActionResult> ClientAvatarMaker(
                            //[ FromQuery(Name = "ApplicationUserId")] Guid? ApplicationUserId,
                            [FromQuery(Name = "start")] int start
                            )
        {
            // var dialogue = _context.Dialogues.Where(x => x.DialogueId == dialogueId).FirstOrDefault();

            int userCounter = 0;
            int counter200 = 0;
            int counter500 = 0;

            var users = _context.ApplicationUsers.Skip(start).Take(100).Select(x => x.Id).ToList();
            foreach (var ApplicationUserId in users)
            {
                userCounter++;
                DateTime date = new DateTime(2019, 09, 20);
                var allDialogues = _context.Dialogues.Where(x => x.ApplicationUserId == ApplicationUserId && x.BegTime <= date).OrderByDescending(x => x.BegTime).ToList();
                //  var atr = _context.FileFrames.Where(item => item.FileName == AvatarFileName).Select(p => p.FrameAttribute.FirstOrDefault()).FirstOrDefault();

                foreach (var dialogue in allDialogues)
                {
                    var frames =
                            _context.FileFrames
                                .Include(p => p.FrameAttribute)
                                .Where(item =>
                                    item.ApplicationUserId == ApplicationUserId
                                    && item.Time >= dialogue.BegTime
                                    && item.Time <= dialogue.EndTime)
                                .ToList();

                    var attributes = frames.Where(p => p.FrameAttribute.Any())
                        .Select(p => p.FrameAttribute.First())
                        .ToList();
                    var AvatarFileName = dialogue.DialogueId.ToString() + ".jpg";
                    // var attributes2 = frames.SelectMany(p => p.FrameAttribute).FirstOrDefault();

                    if (attributes.Count() == 0 && !frames.Any(item => item.FileName == AvatarFileName))
                        continue;


                    FrameAttribute attribute = attributes.First();
                    //if (!string.IsNullOrWhiteSpace(AvatarFileName))
                    //{
                    //    attribute = frames.Where(item => item.FileName == AvatarFileName).Select(p => p.FrameAttribute.FirstOrDefault()).FirstOrDefault();
                    //    attribute = attribute ?? attributes.First();
                    //}
                    //else
                    //{
                    //  attribute = attributes.First();
                    //   }

                    if (await _sftpClient.IsFileExistsAsync($"{_sftpSettings.DestinationPath}" + "clientavatars/" + $"{dialogue.DialogueId}.jpg"))
                    {
                        continue;
                    }
                    if (!await _sftpClient.IsFileExistsAsync($"{_sftpSettings.DestinationPath}" + "frames/" + attribute.FileFrame.FileName))
                    {
                        continue;
                    }
                    var pathClient = new PathClient();
                    var sessionDir = Path.GetFullPath(pathClient.GenLocalDir(pathClient.GenSessionId()));
                    try
                    {
                        //var localPath =
                        //    await _sftpClient.DownloadFromFtpToLocalDiskAsync("frames/" + attribute.FileFrame.FileName, sessionDir);
                        var localPath =
                        await _sftpClient.DownloadFromFtpToLocalDiskAsync("frames/" + attribute.FileFrame.FileName);

                        var faceRectangle = JsonConvert.DeserializeObject<FaceRectangle>(attribute.Value);
                        var rectangle = new Rectangle
                        {
                            Height = faceRectangle.Height,
                            Width = faceRectangle.Width,
                            X = faceRectangle.Top,
                            Y = faceRectangle.Left
                        };

                        var stream = FaceDetection.CreateAvatar(localPath, rectangle);
                        stream.Seek(0, SeekOrigin.Begin);
                        await _sftpClient.UploadAsMemoryStreamAsync(stream, "clientavatars/", $"{dialogue.DialogueId}.jpg");
                        stream.Dispose();
                        stream.Close();
                        _sftpClient.DisconnectAsync();
                    }
                    catch (Exception ex)
                    { return BadRequest(); }
                }
            }
            Dictionary<string, string> result = new Dictionary<string, string>();
            result["success"] = counter200.ToString();
            result["error"] = counter500.ToString();
            result["users"] = userCounter.ToString();


            return Ok();
}


        //[HttpGet("Benchmarks")]
        //public async Task<IActionResult> Benchmarks()
        //{
        //    for (int i = 0; i < 700; i++)
        //    {
        //        DateTime today = DateTime.Now.AddDays(-i).Date;
        //        if (!_context.Benchmarks.Any(x => x.Day.Date == today))
        //        {
        //            var nextDay = today.AddDays(1);
        //            var typeIdCross = _context.PhraseTypes
        //                   .Where(p => p.PhraseTypeText == "Cross")
        //                   .Select(p => p.PhraseTypeId)
        //                   .First();
        //            var dialogues = _context.Dialogues
        //                     .Where(p => p.BegTime.Date == today
        //                             && p.StatusId == 3
        //                             && p.InStatistic == true
        //                             && p.ApplicationUser.Company.CompanyIndustryId != null)
        //                     .Select(p => new DialogueInfo
        //                     {
        //                         IndustryId = p.ApplicationUser.Company.CompanyIndustryId,
        //                         CompanyId = p.ApplicationUser.CompanyId,
        //                         DialogueId = p.DialogueId,
        //                         CrossCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdCross).Count(),
        //                         SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
        //                         BegTime = p.BegTime,
        //                         EndTime = p.EndTime
        //                     })
        //                     .ToList();

        //            var sessions = _context.Sessions
        //                     .Where(p => p.BegTime.Date == today
        //                           && p.StatusId == 7)
        //                   .Select(p => new SessionInfo
        //                   {
        //                       IndustryId = p.ApplicationUser.Company.CompanyIndustryId,
        //                       CompanyId = p.ApplicationUser.CompanyId,
        //                       BegTime = p.BegTime,
        //                       EndTime = p.EndTime
        //                   })
        //                   .ToList();

        //            var benchmarkNames = _context.BenchmarkNames.ToList();
        //            var benchmarkSatisfIndustryAvgId = GetBenchmarkNameId("SatisfactionIndexIndustryAvg", benchmarkNames);
        //            var benchmarkSatisfIndustryMaxId = GetBenchmarkNameId("SatisfactionIndexIndustryBenchmark", benchmarkNames);

        //            var benchmarkCrossIndustryAvgId = GetBenchmarkNameId("CrossIndexIndustryAvg", benchmarkNames);
        //            var benchmarkCrossIndustryMaxId = GetBenchmarkNameId("CrossIndexIndustryBenchmark", benchmarkNames);

        //            var benchmarkLoadIndustryAvgId = GetBenchmarkNameId("LoadIndexIndustryAvg", benchmarkNames);
        //            var benchmarkLoadIndustryMaxId = GetBenchmarkNameId("LoadIndexIndustryBenchmark", benchmarkNames);

        //            if (dialogues.Count() != 0)
        //            {
        //                foreach (var groupIndustry in dialogues.GroupBy(x => x.IndustryId))
        //                {
        //                    var dialoguesInIndustry = groupIndustry.ToList();
        //                    var sessionsInIndustry = sessions.Where(x => x.IndustryId == groupIndustry.Key).ToList();
        //                    //  if (dialoguesInIndustry.Count() != 0 && sessionsInIndustry.Count() != 0)
        //                    {
        //                        var satisfactionIndex = _dbOperation.SatisfactionIndex(dialoguesInIndustry);
        //                        var crossIndex = _dbOperation.CrossIndex(dialoguesInIndustry);
        //                        var loadIndex = _dbOperation.LoadIndex(sessionsInIndustry, dialoguesInIndustry, today, nextDay);
        //                        if (satisfactionIndex != null) AddNewBenchmark((double)satisfactionIndex, benchmarkSatisfIndustryAvgId, today, groupIndustry.Key);
        //                        if (crossIndex != null) AddNewBenchmark((double)crossIndex, benchmarkCrossIndustryAvgId, today, groupIndustry.Key);
        //                        if (loadIndex != null) AddNewBenchmark((double)loadIndex, benchmarkLoadIndustryAvgId, today, groupIndustry.Key);

        //                        var maxSatisfInd = dialoguesInIndustry.GroupBy(x => x.CompanyId).Max(x => _dbOperation.SatisfactionIndex(x.ToList()));
        //                        var maxCrossIndex = dialoguesInIndustry.GroupBy(x => x.CompanyId).Max(x => _dbOperation.CrossIndex(x.ToList()));
        //                        var maxLoadIndex = dialoguesInIndustry.GroupBy(x => x.CompanyId)
        //                            .Max(p =>
        //                            _dbOperation.LoadIndex(
        //                                sessionsInIndustry.Where(s => s.CompanyId == p.Key).ToList(),
        //                                p.ToList(),
        //                                today,
        //                                nextDay));

        //                        if (maxSatisfInd != null) AddNewBenchmark((double)maxSatisfInd, benchmarkSatisfIndustryMaxId, today, groupIndustry.Key);
        //                        if (maxCrossIndex != null) AddNewBenchmark((double)maxCrossIndex, benchmarkCrossIndustryMaxId, today, groupIndustry.Key);
        //                        if (maxLoadIndex != null) AddNewBenchmark((double)maxLoadIndex, benchmarkLoadIndustryMaxId, today, groupIndustry.Key);
        //                    }
        //                }

        //                _context.SaveChanges();
        //            }
        //        }
        //    }
        //    return Ok();
        //}

        //private void AddNewBenchmark(double val, Guid benchmarkNameId, DateTime today, Guid? industryId = null)
        //{
        //    Benchmark benchmark = new Benchmark()
        //    {
        //        IndustryId = industryId,
        //        Value = val,
        //        Weight = 1,// dialoguesInCompany.Count();
        //        Day = today,
        //        BenchmarkNameId = benchmarkNameId
        //    };
        //    _context.Benchmarks.Add(benchmark);
        //}

        //private Guid GetBenchmarkNameId(string name, List<BenchmarkName> benchmarkNames)
        //{
        //    return benchmarkNames.FirstOrDefault(x => x.Name == name).Id;
        //}


        [HttpGet("SendDialogueMake")]
        public async Task<IActionResult> SendDialogueMake(
                                                     [FromQuery(Name = "companyId")] Guid? companyId)
        {

            var begTime = new DateTime(2019, 09, 21);
            var endTime = new DateTime(2019, 09, 24);

            var companyIds = _context.Companys.Where(x => x.CorporationId.ToString() == "72402355-ef7c-41bd-b28e-4234a889c3ba").Select(x => x.CompanyId).ToList();
            var userIds = _context.Users.Where(x => companyIds.Contains((Guid)x.CompanyId)).Select(x => x.Id).ToList();

            // var userIds = _context.Users.Where(x => companyId == (Guid)x.CompanyId).Select(x => x.Id).ToList();
            var dialoguesVideos = _context.FileVideos.Where(x => userIds.Contains(x.ApplicationUserId) && x.BegTime >= begTime && x.EndTime <= endTime)
                .Select(x => x.FileName).ToList();

            string html = string.Empty;
            foreach (var item in dialoguesVideos)
            {
                string url = @"https://heedbookslave.northeurope.cloudapp.azure.com/user/Test/ResendVideoForFraming?fileName=videos%" + item;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.AutomaticDecompression = DecompressionMethods.GZip;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    html = reader.ReadToEnd();
                }
            }
            return Ok(html);
        }


        //[HttpGet("DialogueFrames")]
        //public async Task<IActionResult> DialogueFrames(
        //                                                [FromQuery(Name = "path")] string videoBlobRelativePath)
        //{
        //    try
        //    {
        //        var fileName = Path.GetFileNameWithoutExtension(videoBlobRelativePath);
        //        var applicationUserId = fileName.Split(("_"))[0];
        //        var videoTimeStamp =
        //            DateTime.ParseExact(fileName.Split(("_"))[1], "yyyyMMddHHmmss", CultureInfo.InvariantCulture);

        //        var pathClient = new PathClient();
        //        var sessionDir = Path.GetFullPath(pathClient.GenLocalDir(pathClient.GenSessionId()));

        //        var ffmpeg = new FFMpegWrapper(
        //            new FFMpegSettings
        //            {
        //                FFMpegPath = Path.Combine(pathClient.BinPath(), "ffmpeg.exe")
        //            });

        //        await _sftpClient.DownloadFromFtpToLocalDiskAsync(
        //                $"{_sftpSettings.DestinationPath}{videoBlobRelativePath}", sessionDir);
        //        var localFilePath = Path.Combine(sessionDir, Path.GetFileName(videoBlobRelativePath));

        //        var splitRes = ffmpeg.SplitToFrames(localFilePath, sessionDir);
        //        var frames = Directory.GetFiles(sessionDir, "*.jpg")
        //             .OrderBy(p => Convert.ToInt32((Path.GetFileNameWithoutExtension(p))))
        //             .Select(p => new FrameInfo
        //             {
        //                 FramePath = p,
        //             })
        //             .ToList();
        //                for (int i = 0; i < frames.Count(); i++)
        //                {
        //                    frames[i].FrameTime = videoTimeStamp.AddSeconds(i * 3).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        //                    frames[i].FrameName = $"{applicationUserId}_{frames[i].FrameTime}.jpg";
        //                }

        //        //var tasks = frames.Select(p => {
        //        //    return Task.Run(async () =>
        //        //    {
        //        //        await _sftpClient.UploadAsync(p.FramePath, "frames", p.FrameName);
        //        //    });
        //        //});
        //        //await Task.WhenAll(tasks);
        //        foreach (var p in frames)
        //        {
        //            await _sftpClient.UploadAsync(p.FramePath, "frames", p.FrameName);
        //        }

        //        _log.Info($"TEST FRAME "+ videoBlobRelativePath);
        //        foreach (var frame in frames)
        //        {
        //            var fileFrame = new FileFrame
        //            {
        //                FileFrameId = Guid.NewGuid(),
        //                ApplicationUserId = Guid.Parse(applicationUserId),
        //                FaceLength = 0,
        //                FileContainer = "frames",
        //                FileExist = true,
        //                FileName = frame.FrameName,
        //                IsFacePresent = false,
        //                StatusId = 6,
        //                StatusNNId = 6,
        //                Time = DateTime.ParseExact(frame.FrameTime, "yyyyMMddHHmmss", CultureInfo.InvariantCulture)
        //            };
        //            _context.FileFrames.Add(fileFrame);
        //            _context.SaveChanges();
        //            _log.Info($"Creating frame - {frame.FrameName}");
        //            string FrameContainerName = "frames";
        //            //var message = new FaceAnalyzeRun
        //            //{
        //            //    Path = $"{FrameContainerName}/{frame.FrameName}"
        //            //};
        //            var remotePath = $"{_sftpSettings.DestinationPath}{FrameContainerName}/{frame.FrameName}";

        //            try
        //            {
        //                _log.Info($"Function started");
        //                if (await _sftpClient.IsFileExistsAsync(remotePath))
        //                {
        //                    string localPath = default;
        //                    lock (_syncRoot)
        //                    {
        //                        localPath = _sftpClient.DownloadFromFtpToLocalDiskAsync(remotePath, "D://1/").GetAwaiter().GetResult();
        //                    }
        //                    _log.Info($"Download to path - {localPath}");
        //                    if (FaceDetection.IsFaceDetected(localPath, out var faceLength))
        //                    {
        //                        _log.Info($"{localPath}: Face detected!");

        //                        var byteArray = await System.IO.File.ReadAllBytesAsync(localPath);
        //                        var base64String = Convert.ToBase64String(byteArray);

        //                        var faceResult = await _client.GetFaceResult(base64String);
        //                        _log.Info($"Face result is {JsonConvert.SerializeObject(faceResult)}");
        //                        var fileName1 = localPath.Split('/').Last();
        //                        FileFrame fileFrame1;
        //                        lock (_context)
        //                        {
        //                            fileFrame = _context.FileFrames.Where(entity => entity.FileName == fileName1).FirstOrDefault();
        //                        }
        //                        if (fileFrame != null && faceResult.Any())
        //                        {
        //                            var frameEmotion = new FrameEmotion
        //                            {
        //                                FileFrameId = fileFrame.FileFrameId,
        //                                AngerShare = faceResult.Average(item => item.Emotions.Anger),
        //                                ContemptShare = faceResult.Average(item => item.Emotions.Contempt),
        //                                DisgustShare = faceResult.Average(item => item.Emotions.Disgust),
        //                                FearShare = faceResult.Average(item => item.Emotions.Fear),
        //                                HappinessShare = faceResult.Average(item => item.Emotions.Happiness),
        //                                NeutralShare = faceResult.Average(item => item.Emotions.Neutral),
        //                                SadnessShare = faceResult.Average(item => item.Emotions.Sadness),
        //                                SurpriseShare = faceResult.Average(item => item.Emotions.Surprise),
        //                                YawShare = faceResult.Average(item => item.Headpose.Yaw)
        //                            };

        //                            var frameAttribute = faceResult.Select(item => new FrameAttribute
        //                            {
        //                                Age = item.Attributes.Age,
        //                                Gender = item.Attributes.Gender,
        //                                Descriptor = JsonConvert.SerializeObject(item.Descriptor),
        //                                FileFrameId = fileFrame.FileFrameId,
        //                                Value = JsonConvert.SerializeObject(item.Rectangle)
        //                            }).FirstOrDefault();

        //                            fileFrame.FaceLength = faceLength;
        //                            fileFrame.IsFacePresent = true;

        //                            if (frameAttribute != null) _context.FrameAttributes.Add(frameAttribute);
        //                            _context.FrameEmotions.Add(frameEmotion);
        //                            lock (_context)
        //                            {
        //                                _context.SaveChanges();
        //                            }
        //                        }
        //                    }
        //                    else
        //                    {
        //                        _log.Info($"{localPath}: No face detected!");
        //                    }
        //                    _log.Info("Function finished");

        //                    System.IO.File.Delete(localPath);
        //                }
        //                else
        //                {
        //                    _log.Info($"No such file {remotePath}");
        //                }

        //                _log.Info("Function face analyze finished");

        //            }
        //            catch (SftpPathNotFoundException e)
        //            {
        //                _log.Fatal($"{e}");
        //            }
        //            catch (Exception e)
        //            {
        //                _log.Fatal($"Exception occured {e}");
        //              //  throw new FaceAnalyzeServiceException(e.Message, e);
        //            }
        //        }
        //      //  _log.Info("Deleting local files");
        //        Directory.Delete(sessionDir, true);

        //        System.Console.WriteLine("Function finished");
        //        _log.Info($"TEST FRAME " + videoBlobRelativePath+ "  FINISHED");
        //        return Ok();
        //    }
        //    catch (Exception e)
        //    {
        //     //   _log.Fatal($"Exception occured {e}");
        //        System.Console.WriteLine($"{e}");
        //        return BadRequest(e.Message);
        //    }
        //}   



        [HttpGet("SatisfByClient")]
        public async Task<IActionResult> SatisfByClient(Guid dialogueId)
        {
            var dialogue = _context.Dialogues
                  .FirstOrDefault(p => p.DialogueId == dialogueId);
            try
            {
                var campaignContentIds = _context.SlideShowSessions
                        .Where(p => p.BegTime >= dialogue.BegTime
                                && p.BegTime <= dialogue.EndTime
                                && p.ApplicationUserId == dialogue.ApplicationUserId
                                && p.IsPoll)
                        .Select(p => p.CampaignContentId).ToList();

                Func<string, double> intParse = (string answer) =>
                {
                    switch (answer)
                    {
                        case "EMOTION_ANGRY":
                            return 0.1;
                        case "EMOTION_BAD":
                            return 2.5;
                        case "EMOTION_NEUTRAL":
                            return 5;
                        case "EMOTION_GOOD":
                            return 7.5;
                        case "EMOTION_EXCELLENT":
                            return 10;
                        default:
                            {
                                Int32.TryParse(answer, out int res);
                                return Convert.ToDouble(res);
                            }
                    }

                };
                var pollAnswersAvg = _context.CampaignContentAnswers
                      .Where(x => campaignContentIds.Contains(x.CampaignContentId)
                          && x.Time >= dialogue.BegTime
                          && x.Time <= dialogue.EndTime
                          && x.ApplicationUserId == dialogue.ApplicationUserId).ToList()
                      .Select(x => intParse(x.Answer))
                      .Where(res => res != 0)
                      .Average() * 10;
                return Ok(pollAnswersAvg > 100 ? 100 : pollAnswersAvg);
            }
            catch
            {
                return Ok();
            }
        }

        [HttpGet("CheckSessions")]
        public async Task<IActionResult> CheckSessions()
        {
            var sessions = _context.Sessions.Where(x=> x.StatusId==7 ).ToList();
            var grouping = sessions.GroupBy(x => x.ApplicationUserId);

            foreach (var item in grouping)
            {
                var sesInUser = item.OrderBy(x => x.BegTime).ToArray();
                for (int i = 0; i < sesInUser.Count()-1; i++)
                {
                    if(sesInUser[i+1].BegTime < sesInUser[i].EndTime)
                    {
                        if (sesInUser[i + 1].EndTime < sesInUser[i].EndTime)
                        {
                            sesInUser[i + 1].StatusId = 8;
                            i++;
                        }
                        else
                        {
                            sesInUser[i].EndTime = sesInUser[i + 1].BegTime;
                            i++;
                        }
                    }
                    
                }
            }

            _context.SaveChanges();
            return Ok();
        }
        [HttpGet("CheckDialogues2")]
        public async Task<IActionResult> CheckDialogues2()
        {
            var sessions = _context.Sessions.Where(x => x.StatusId == 7).ToList();
            var dialogues = _context.Dialogues.Where(x => x.StatusId == 3 && x.InStatistic == true).ToList();
            var grouping = sessions.GroupBy(x => x.ApplicationUserId);
            int counter = 0;

            foreach (var item in grouping)
            {
                var sesInUser = item.OrderBy(x => x.BegTime).ToArray();
                var dialoguesUser = dialogues.Where(x => x.ApplicationUserId == item.Key).ToList();
                foreach (var dialogue in dialoguesUser)
                {
                    if(!sesInUser.Any(x => dialogue.BegTime >= x.BegTime && dialogue.EndTime <= x.EndTime))
                    {
                        if (!sesInUser.Any(x => dialogue.BegTime >= x.BegTime && dialogue.BegTime <= x.EndTime))
                        {
                            if (!sesInUser.Any(x => dialogue.EndTime >= x.BegTime && dialogue.EndTime <= x.EndTime))
                            {
                                counter++;
                            }
                            }
                        }
                }
            }
            return Ok(counter);
        }


        [HttpGet("CheckDialogues")]
        public async Task<IActionResult> CheckDialogues()
        {
            var sessions = _context.Sessions.Where(x => x.StatusId == 7).ToList();
            var dialogues = _context.Dialogues.Where(x => x.StatusId == 3).ToList();
            var grouping = sessions.GroupBy(x => x.ApplicationUserId);
            int counter = 0;

            foreach (var item in grouping)
            {
                var sesInUser = item.OrderBy(x => x.BegTime).ToArray();
                var dialoguesUser = dialogues.Where(x => x.ApplicationUserId == item.Key).ToList();
                foreach (var dialogue in dialoguesUser)
                {
                    if (sesInUser.Any(x => x.BegTime <= dialogue.BegTime && dialogue.EndTime <= x.EndTime ))
                        continue;
                    if(sesInUser.Any(x => x.BegTime <= dialogue.BegTime && dialogue.BegTime <= x.EndTime ))
                    {
                        //---початок потрапив до сесії
                        //var session = sesInUser?.FirstOrDefault(x => x.BegTime <= dialogue.BegTime && dialogue.BegTime <= x.EndTime);
                        //var nextSession = sesInUser?.FirstOrDefault(x => x.BegTime > session.BegTime);
                        //if (nextSession.BegTime < dialogue.EndTime)
                        //{
                        //   nextSession.BegTime = dialogue.EndTime;
                        //}
                        //    session.EndTime = dialogue.EndTime.AddSeconds(1);
                    }
                    else if (sesInUser.Any(x => x.BegTime <= dialogue.EndTime && dialogue.EndTime <= x.EndTime))
                    {
                        //---кінець потрапив до сесії
                        //var session = sesInUser?.FirstOrDefault(x => x.BegTime <= dialogue.EndTime && dialogue.EndTime <= x.EndTime);
                        //var prevSession = sesInUser?.FirstOrDefault(x => x.BegTime < session.BegTime);
                        //if (prevSession.EndTime > dialogue.BegTime)
                        //{
                        //   prevSession.EndTime = dialogue.BegTime;
                        //}
                        //    session.BegTime = dialogue.BegTime.AddSeconds(-1);
                    }
                    else
                    {
                        var session = new Session
                        {
                            BegTime = dialogue.BegTime.AddSeconds(-1),
                            EndTime = dialogue.EndTime.AddSeconds(1),
                            ApplicationUserId = dialogue.ApplicationUserId,
                            StatusId = 7,
                            IsDesktop = true
                        };
                        _context.Sessions.Add(session);
                        counter++;
                    }
                }

                }
            

            _context.SaveChanges();
            return Ok();
        }


        //[HttpGet("Help3")]
        //public async Task<IActionResult> Help3()
        //{
        //    var connectionString = "User ID = postgres; Password = annushka123; Host = 127.0.0.1; Port = 5432; Database = onprem_backup; Pooling = true; Timeout = 120; CommandTimeout = 0";
        //    DbContextOptionsBuilder<RecordsContext> dbContextOptionsBuilder = new DbContextOptionsBuilder<RecordsContext>();
        //    dbContextOptionsBuilder.UseNpgsql(connectionString,
        //           dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(UserOperations)));
        //    var localContext = new RecordsContext(dbContextOptionsBuilder.Options);
        //    var contentInBackup = localContext.Contents.FirstOrDefault();
        //    Guid contentPrototypeId = new Guid("07565966-7db2-49a7-87d4-1345c729a6cb");
        //    var content = _context.Contents.FirstOrDefault(x => x.ContentId == contentPrototypeId);
        //    contentInBackup.CreationDate = content.CreationDate;
        //    contentInBackup.JSONData = content.JSONData;
        //    contentInBackup.RawHTML = content.RawHTML;
        //    contentInBackup.UpdateDate = content.UpdateDate;
        //    localContext.SaveChanges();
        //    return Ok();
        //}


        [HttpGet("DatabaseFilling")]
        public string DatabaseFilling
        (
            [FromQuery]string countryName = null,
            [FromQuery]string companyIndustryName = null,
            [FromQuery]string corporationName = null,
            [FromQuery]string languageName = null,
            [FromQuery]string languageShortName = null)
        {
            // add country
            if (countryName != null)
            {
                var countryId = Guid.NewGuid();
                var country = new Country
                {
                    CountryId = countryId,
                    CountryName = countryName,
                };
                _context.Countrys.Add(country);
                _context.SaveChanges();
            }

            // add language
            if (languageName != null && languageShortName != null)
            {
                var language = new Language
                {
                    // LanguageId = 1,
                    LanguageName = languageName,
                    LanguageLocalName = languageName,
                    LanguageShortName = languageShortName
                };
                _context.Languages.Add(language);
                _context.SaveChanges();
            }

            // create company industry
            if (companyIndustryName != null)
            {
                var companyIndustryId = Guid.NewGuid();
                var companyIndustry = new CompanyIndustry
                {
                    CompanyIndustryId = companyIndustryId,
                    CompanyIndustryName = companyIndustryName,
                    CrossSalesIndex = 100,
                    LoadIndex = 100,
                    SatisfactionIndex = 100
                };
                _context.CompanyIndustrys.Add(companyIndustry);
                _context.SaveChanges();
            }

            // create new corporation
            if (corporationName != null)
            {
                var corporationId = Guid.NewGuid();
                var corp = new Corporation
                {
                    Id = corporationId,
                    Name = corporationName
                };
                _context.Corporations.Add(corp);
                _context.SaveChanges();
            }

            //     add statuss
            List<string> statuses = new List<string>(new string[] { "Online", "Offline", "Active", "Disabled", "Inactive", "InProgress", "Finished", "Error", "Pending disabled", "Trial", "AutoActive", "AutoFinished", "AutoError" });


            for (int i = 1; i < statuses.Count() + 1; i++)
            {
                var status = new Status
                {
                    StatusId = i,
                    StatusName = statuses[i]
                };
                _context.Statuss.Add(status);
                _context.SaveChanges();
            }
            return "OK";
        }

        [HttpGet("newtest")]
        public IActionResult newtest()
        {
            var dialogues = _context.DialogueClientSatisfactions.Where(p => p.MeetingExpectationsTotal < 35).ToList();
            var random = new Random();
            foreach (var dialogue in dialogues)
            {
                dialogue.MeetingExpectationsTotal =  Math.Max((double) dialogue.MeetingExpectationsTotal, 35 + random.Next(10));
            }
            _context.SaveChanges();
            // var dialogue = _context.Dialogues.Where(p => p.DialogueId.ToString() == "5d90051a-15a9-4126-8988-6e7f6ab256e1").FirstOrDefault();
            // dialogue.StatusId = 8;
            // _context.SaveChanges();
            return Ok();
        }

        [HttpGet("phrase")]
        public IActionResult phrase()
        {
            var pathPhrase = "/home/nikolay/Desktop/phrase.json";
            var pathCompanyPhrase = "/home/nikolay/Desktop/phrasecompanys.json";
            List<Phrase> phrases;
            List<PhraseCompany> companyPhrases;
            using (StreamReader r = new StreamReader(pathPhrase))
            {
                phrases = JsonConvert.DeserializeObject<List<Phrase>>(r.ReadToEnd());
            }
             using (StreamReader r = new StreamReader(pathCompanyPhrase))
            {
                companyPhrases = JsonConvert.DeserializeObject<List<PhraseCompany>>(r.ReadToEnd());
            }
            _context.Phrases.AddRange(phrases);
            _context.PhraseCompanys.AddRange(companyPhrases);
            _context.SaveChanges();
            return Ok();
        }

        [HttpGet("samedialogues")]
        public IActionResult samedialogues()
        {
            var begTime = DateTime.Now.AddDays(-10);
            var dialogues = _context.Dialogues.Where(p => p.BegTime >= begTime && p.StatusId == 3)
                .ToList()
                .OrderBy(p => p.BegTime)
                .ToList();

            for (int i = 1; i< dialogues.Count(); i++)
            {
                if (dialogues[i].BegTime == dialogues[i-1].BegTime && dialogues[i].ApplicationUserId == dialogues[i-1].ApplicationUserId)
                {
                    System.Console.WriteLine(dialogues[i].DialogueId);
                    System.Console.WriteLine(dialogues[i-1].DialogueId);

                    dialogues[i-1].StatusId = 8;
                }
                _context.SaveChanges();
            }
            return Ok();
        }
        
        [HttpGet("test")]
        public IActionResult test()
        {
           
            var begTime = DateTime.Now.AddDays(-10);
            // var frameLast = _context.FileFrames.Where(p => p.FileName == "f62f320f-e448-40a1-90d3-9af1c745303d_20190709150711.jpg").FirstOrDefault();
            // var begTime = frameLast.Time;
            // var EndTime = DateTime.UtcNow.AddHours(-13);
            // var frames = _context.FileFrames.Where(p => p.Time >= begTime && p.Time <= EndTime && p.FaceLength == 0).ToList().OrderBy(p => p.Time).ToList();
            // System.Console.WriteLine($"{frames.Count()}");
            // var i = 0;
            // foreach (var frame in frames.Select(p => p.FileName).ToList().Distinct().ToList())
            // {
            //     System.Console.WriteLine($"Index - {i}, frame - {frame}");
            //     i++;
            //     var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://slavehb.northeurope.cloudapp.azure.com/user/Face");
            //     httpWebRequest.ContentType = "application/json";
            //     httpWebRequest.Method = "POST";

            //     using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            //     {
            //        var dict = new Dictionary<string, string>();
            //        dict["Path"] = $"frames/{frame}";

            //         streamWriter.Write(JsonConvert.SerializeObject(dict));
            //     }

            //     var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            //     using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            //     {
            //         var result = streamReader.ReadToEnd();
            //         System.Console.WriteLine("Result" + result);
            //     }

            //     Thread.Sleep(300);
            // }




            //var dialogues = _context.Dialogues.Where(p => p.StatusId == 8 && p.BegTime >= begTime).ToList();
            //System.Console.WriteLine(dialogues.Count());
            //dialogues = dialogues.Where(p => p.Comment == null || !p.Comment.StartsWith("Too many holes in dialogue")).ToList();
            //System.Console.WriteLine(dialogues.Count());
            //var i = 0;
            //foreach (var dialogue in dialogues)
            //{
            //    try
            //    {
            //        var url = $"https://slavehb.northeurope.cloudapp.azure.com/user/DialogueRecalculate/CheckRelatedDialogueData?DialogueId={dialogue.DialogueId}";
            //        System.Console.WriteLine($"Processing {dialogue.DialogueId}, Index {i}");

            //        var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            //        httpWebRequest.ContentType = "application/json";
            //        httpWebRequest.Method = "POST";

            //        var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            //        using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            //        {
            //            var result = streamReader.ReadToEnd();
            //            System.Console.WriteLine("Result ---- " + result);
            //        }
            //        Thread.Sleep(1000);
            //        i++;
            //    }
            //    catch (Exception e)
            //    {
                    
            //    }
            
            //}


            // System.Console.WriteLine(audios.Select(p => p.DialogueId).Distinct().ToList().Count());

            // foreach (var audio in audios.Select(p => p.DialogueId).Distinct().ToList())
            // {
            //     System.Console.WriteLine($"Processing {audio}");
            //     var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://slavehb.northeurope.cloudapp.azure.com/user/AudioAnalyze/audio-analyze");
            //     httpWebRequest.ContentType = "application/json";
            //     httpWebRequest.Method = "POST";

            //     using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            //     {
            //        var dict = new Dictionary<string, string>();
            //        dict["Path"] = $"dialogueaudios/{audio}.wav";

            //         streamWriter.Write(JsonConvert.SerializeObject(dict));
            //     }

            //     var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            //     using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            //     {
            //         var result = streamReader.ReadToEnd();
            //         System.Console.WriteLine("Result" + result);
            //     }

            //     Thread.Sleep(1500);    
            // }
           




            // var res = frames.GroupBy(p => p.Time).Select(p => new {
            //     Time = p.Key,
            //     Count = p.Count()
            // });

            // var dialogueHint = new DialogueHint{
            //     DialogueHintId = Guid.NewGuid(),
            //     DialogueId = dialogueId,
            //     HintText = "Следите за настроением клиента. Если возникла негативная обстановка, постарайтесь ее разрядить.",
            //     IsAutomatic = true,
            //     Type = "Text",
            //     IsPositive = false
            // };

            // var dialogueHint2 = new DialogueHint{
            //     DialogueHintId = Guid.NewGuid(),
            //     DialogueId = dialogueId,
            //     HintText = "Делайте дополнительные предложения. Ищите подход к клиенту. Попробуйте расположить к себе клиента.",
            //     IsAutomatic = true,
            //     Type = "Text",
            //     IsPositive = false
            // };

            // _context.DialogueHints.Add(dialogueHint);
            // _context.DialogueHints.Add(dialogueHint2);
            // _context.SaveChanges();



            // var frames = _context.FileFrames.Where(p => p.Time >= dialogue.BegTime && p.Time <= dialogue.EndTime).ToList();
            // var framesIds = frames.Select(p => p.FileFrameId).ToList();
            // var framesAtr = _context.FrameAttributes.Where(p => framesIds.Contains(p.FileFrameId)).ToList();
            // var framesEm = _context.FrameEmotions.Where(p => framesIds.Contains(p.FileFrameId)).ToList();

            // _context.FrameAttributes.RemoveRange(framesAtr);
            // _context.SaveChanges();

            // _context.FrameEmotions.RemoveRange(framesEm);
            // _context.SaveChanges();

            //  _context.FileFrames.RemoveRange(frames);
            // var words = _context.DialogueWords.Where(p => p.DialogueWordId.ToString() == "176b5d3a-2804-4cf5-91fd-3a609651e0f6").ToList();
            // _context.RemoveRange(words);


            // var mood = _context.DialogueClientSatisfactions.Where(p => p.DialogueId == dialogueId).First();
            // mood.MeetingExpectationsTotal = 46;



            return Ok();

        }
    }

    public class FrameInfo
    {
        public string FramePath;
        public string FrameTime;
        public string FrameName;
    }
}