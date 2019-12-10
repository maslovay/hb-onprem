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
using System.Transactions;
using FillingSatisfactionService.Helper;
using HBData.Repository;

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
        private readonly IMailSender _mailSender;
        private readonly IRequestFilters _requestFilters;
        private readonly SftpSettings _sftpSettings;
        private readonly ElasticClient _log;
        private readonly IDBOperations _dbOperation;
        private readonly IGenericRepository _repository;
        //   private readonly INotificationHandler _handler;
        //    private readonly HbMlHttpClient _client;

        private readonly Object _syncRoot = new Object();


        public HelpController(
            IConfiguration config,
            ILoginService loginService,
            RecordsContext context,
            SftpClient sftpClient,
            IMailSender mailSender,
            IRequestFilters requestFilters,
            SftpSettings sftpSettings,
            ElasticClient log,
            IDBOperations dBOperations,
            IGenericRepository repository
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
            _repository = repository;
            //   _handler = handler;
            //   _client = client ?? throw new ArgumentNullException(nameof(client));
        }




        [HttpGet("FindTheSameDialogues")]
        public async Task<IActionResult> FindTheSameDialogues()
        {
            var date = new DateTime(2019, 10, 01);
            var users = _context.ApplicationUsers.Include(x => x.Dialogue).OrderBy(x => x.CreationDate).ToList();
            int userC = 0;
            int counterInDialogue = 0;
            Dialogue dialogueForRemove = null;
            userC = 0;
            foreach (var user in users)
            {
                userC++;
                var dialogues = user.Dialogue.Where(x => x.StatusId == 3 && x.BegTime >= date).OrderBy(x => x.BegTime).ToList();
                foreach (var d1 in dialogues)
                {
                    var d2 = dialogues
                        .Where(x => x.DialogueId != d1.DialogueId).ToList()
                        .FirstOrDefault(x => (x.BegTime < d1.BegTime && d1.EndTime < x.EndTime) || (x.BegTime < d1.BegTime && d1.BegTime < x.EndTime)
                        || (x.BegTime < d1.EndTime && d1.EndTime < x.EndTime));
                    if (d2 != null)
                    {
                        if (!await _sftpClient.IsFileExistsAsync($"{_sftpSettings.DestinationPath}" + "dialoguevideos/" + $"{d1.DialogueId}.mkv"))
                        {
                            dialogueForRemove = d1;
                        }
                        else if (!await _sftpClient.IsFileExistsAsync($"{_sftpSettings.DestinationPath}" + "dialoguevideos/" + $"{d2.DialogueId}.mkv"))
                        {
                            dialogueForRemove = d2;
                        }
                        else
                        {
                            var time1 = d1.EndTime.Subtract(d1.BegTime).TotalHours;
                            var time2 = d2.EndTime.Subtract(d2.BegTime).TotalHours;
                            if (time1 > time2)
                            {
                                dialogueForRemove = d2;
                            }
                            else
                            {
                                dialogueForRemove = d1;
                            }
                        }
                        dialogueForRemove.StatusId = 8;
                        if (dialogueForRemove.Comment == null)
                            dialogueForRemove.Comment = "repeat dialogue";

                        _context.SaveChanges();
                        counterInDialogue++;
                        break;
                    }
                }
            }
            return Ok(counterInDialogue);
        }
        //----PART 1---
        [HttpGet("FindTheSessionInSession")]
        public IActionResult FindTheSessionInSession()
        {
            var dateEnd = new DateTime(2019, 11, 01);
            var date = new DateTime(2019, 01, 01);
            var users = _context.ApplicationUsers.Include(x => x.Session).Include(x => x.Dialogue).OrderBy(x => x.CreationDate).ToList();
            int userC = 0;
            int counterInSes = 0;
            userC = 0;
            foreach (var user in users)
            {
                userC++;
                var sessions = user.Session.Where(x => x.StatusId == 7 && x.BegTime >= date && x.BegTime <= dateEnd).OrderBy(x => x.BegTime).ToList();
                foreach (var s1 in sessions)
                {
                    //---FIRST PART---
                    //---CHECK SESSION IN SESSION
                    var s2 = sessions
                        .Where(x => x.SessionId != s1.SessionId).ToList()
                        .FirstOrDefault(x => (x.BegTime <= s1.BegTime && s1.EndTime <= x.EndTime));//сессія в середині іншої сессії

                    if (s2 != null && s1.StatusId != 8)
                    {
                        s1.StatusId = 8;//error                        
                        counterInSes++;
                    }
                }
            }
            _context.SaveChanges();
            return Ok(counterInSes);
        }
        //---STEP 2---
        [HttpGet("FindTheSessionsOneOnAnother")]
        public IActionResult FindTheSessionsOneOnAnother()
        {
            var dateEnd = new DateTime(2019, 11, 01);
            var date = new DateTime(2019, 01, 01);
            var users = _context.ApplicationUsers.Include(x => x.Session).Include(x => x.Dialogue).OrderBy(x => x.CreationDate).ToList();
            int userC = 0;
            int counterInSes = 0;
            userC = 0;
            foreach (var user in users)
            {
                userC++;
                var sessions = user.Session.Where(x => x.StatusId == 7 && x.BegTime >= date && x.BegTime <= dateEnd).OrderBy(x => x.BegTime).ToList();
                foreach (var s1 in sessions)
                {
                    var s2 = sessions
                     .Where(x => x.SessionId != s1.SessionId).ToList()
                     .FirstOrDefault(x => (x.BegTime < s1.BegTime && s1.BegTime < x.EndTime));//---сессiя s1 почалась в середины іншої (s2) сессії а закінчилась пізніше

                    if (s2 != null)
                    {
                        s1.BegTime = s2.BegTime;
                        s2.StatusId = 8;
                        counterInSes++;
                    }
            }
            }
            _context.SaveChanges();
            return Ok(counterInSes);
        }
        [HttpGet("FindDialoguesWithoutSessions")]
        public IActionResult FindDialoguesWithoutSessions()
        {
            var date = new DateTime(2019, 01, 01);
            var users = _context.ApplicationUsers.Include(x => x.Dialogue).Include(x => x.Session).OrderBy(x => x.CreationDate).ToList();
            int counter = 0;
            //var userC = 0;
            foreach (var user in users)
            {
                    var dialogues = user.Dialogue.Where(x => x.StatusId == 3 && x.InStatistic == true).OrderBy(x => x.BegTime).ToList();
                    var sessions = user.Session.OrderBy(x => x.BegTime).ToList();

                    foreach (var dial in dialogues)
                    {
                    //---NO ANY SESSION INCLUDED DIALOGUE
                    if (!sessions.Any(ses =>
                           (ses.BegTime <= dial.BegTime && ses.EndTime >= dial.EndTime)
                        || (ses.BegTime >= dial.BegTime && ses.BegTime <= dial.EndTime)
                        || (ses.EndTime >= dial.BegTime && ses.EndTime <= dial.EndTime)))
                        {
                            Session newSession = new Session()
                            {
                                ApplicationUserId = dial.ApplicationUserId,
                                BegTime = dial.BegTime,
                                EndTime = dial.EndTime,
                                IsDesktop = true,
                                StatusId = 7
                            };
                        _context.Sessions.Add(newSession);
                        counter++;
                        }

                    }
                        //if (!sessions.Any(ses => dial.BegTime >= ses.BegTime && dial.EndTime <= ses.EndTime))//---діалог в середині сессії відсутній
                        //{
                        //if (!sessions.Any(ses => dial.BegTime >= ses.BegTime && dial.BegTime <= ses.EndTime))//---є діалог що почався в сессії
                        //{

                        //}
                        //else if (!sessions.Any(ses => dial.EndTime >= ses.BegTime && dial.EndTime <= ses.EndTime))//---є діалог що закінчився в сессії
                        //{ 

                        //    var nextSession = sessions.Where(x => x.BegTime >= dial.BegTime).FirstOrDefault();
                        //    if ((nextSession.BegTime - dial.BegTime).TotalHours < 6)
                        //    {
                        //        counter++;
                        //        nextSession.BegTime = dial.BegTime;
                        //        _context.SaveChanges();
                        //    }
                        //}
                        //}
                  //  }
            }
            _context.SaveChanges();
            return Ok(counter);
        }

        [HttpGet("ClientAvatarMaker")]
        public async Task<IActionResult> ClientAvatarMaker(
                 //[ FromQuery(Name = "take")] int take,
                 //[FromQuery(Name = "start")] int start
                 [FromQuery(Name = "userId")] Guid userId
                            )
        {
            int userCounter = 0;
            int counter200 = 0;
            int counter500 = 0;

            //var users = _context.ApplicationUsers.Skip(start).Take(take).Select(x => x.Id).ToList();
            //foreach (var ApplicationUserId in users)
            //{
            //int existCounter = 0;
            int noFrames = 0;
            DateTime dateEnd = new DateTime(2019, 09, 21);
            DateTime dateBeg = new DateTime(2019, 08, 01);
            var allDialogues = _context.Dialogues.Where(x => x.ApplicationUserId == userId && x.BegTime <= dateEnd && x.BegTime >= dateBeg).OrderByDescending(x => x.BegTime).ToList();
            //  var atr = _context.FileFrames.Where(item => item.FileName == AvatarFileName).Select(p => p.FrameAttribute.FirstOrDefault()).FirstOrDefault();

            foreach (var dialogue in allDialogues)
            {
                userCounter++;
                try
                {
                    if (await _sftpClient.IsFileExistsAsync($"{_sftpSettings.DestinationPath}" + "clientavatars/" + $"{dialogue.DialogueId}.jpg"))
                    {
                        continue;
                    }
                }
                catch
                {
                    counter500++;
                }

                var frames =
                        _context.FileFrames
                            .Include(p => p.FrameAttribute)
                            .Where(item =>
                                item.ApplicationUserId == userId
                                && item.Time >= dialogue.BegTime
                                && item.Time <= dialogue.EndTime)
                            .ToList();

                var attributes = frames.Where(p => p.FrameAttribute.Any())
                    .Select(p => p.FrameAttribute.First())
                    .ToList();
                if (attributes.Count() == 0)
                    continue;


                FrameAttribute attribute = attributes.First();
                if (!await _sftpClient.IsFileExistsAsync($"{_sftpSettings.DestinationPath}" + "frames/" + attribute.FileFrame.FileName))
                {
                    noFrames++;
                    if (noFrames == 50) break;
                    continue;
                }
                var pathClient = new PathClient();
                var sessionDir = Path.GetFullPath(pathClient.GenLocalDir(pathClient.GenSessionId()));
                try
                {
                    var localPath =
                        await _sftpClient.DownloadFromFtpToLocalDiskAsync("frames/" + attribute.FileFrame.FileName, sessionDir);
                    //var localPath =
                    //await _sftpClient.DownloadFromFtpToLocalDiskAsync("frames/" + attribute.FileFrame.FileName);

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
                    await _sftpClient.DisconnectAsync();
                    Directory.Delete(sessionDir, true);
                    counter200++;
                }
                catch
                {
                    counter500++;
                }
            }
            //    }
            Dictionary<string, string> result = new Dictionary<string, string>
            {
                ["success"] = counter200.ToString(),
                ["error"] = counter500.ToString(),
                ["users"] = userCounter.ToString()
            };


            return Ok(result);
        }
   
        [HttpGet("CheckSessions")]
        public IActionResult CheckSessions()
        {
            var sessions = _context.Sessions.Where(x => x.StatusId == 7).ToList();
            var grouping = sessions.GroupBy(x => x.ApplicationUserId);

            foreach (var item in grouping)
            {
                var sesInUser = item.OrderBy(x => x.BegTime).ToArray();
                for (int i = 0; i < sesInUser.Count() - 1; i++)
                {
                    if (sesInUser[i + 1].BegTime < sesInUser[i].EndTime)
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
        public IActionResult CheckDialogues2()
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
                    if (!sesInUser.Any(x => dialogue.BegTime >= x.BegTime && dialogue.EndTime <= x.EndTime))
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
        public IActionResult CheckDialogues()
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
                    if (sesInUser.Any(x => x.BegTime <= dialogue.BegTime && dialogue.EndTime <= x.EndTime))
                        continue;
                    if (sesInUser.Any(x => x.BegTime <= dialogue.BegTime && dialogue.BegTime <= x.EndTime))
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

        [HttpGet("newtest")]
        public IActionResult Newtest()
        {
            var dialogues = _context.DialogueClientSatisfactions.Where(p => p.MeetingExpectationsTotal < 35).ToList();
            var random = new Random();
            foreach (var dialogue in dialogues)
            {
                dialogue.MeetingExpectationsTotal = Math.Max((double)dialogue.MeetingExpectationsTotal, 35 + random.Next(10));
            }
            _context.SaveChanges();
            // var dialogue = _context.Dialogues.Where(p => p.DialogueId.ToString() == "5d90051a-15a9-4126-8988-6e7f6ab256e1").FirstOrDefault();
            // dialogue.StatusId = 8;
            // _context.SaveChanges();
            return Ok();
        }


      

        [HttpGet("test")]
        public IActionResult Test()
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