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
        public async Task<IActionResult> FindTheSessionInSession()
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
        public async Task<IActionResult> FindTheSessionsOneOnAnother()
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
        public async Task<IActionResult> FindDialoguesWithoutSessions()
        {
            var date = new DateTime(2019, 01, 01);
            var users = _context.ApplicationUsers.Include(x => x.Dialogue).Include(x => x.Session).OrderBy(x => x.CreationDate).ToList();
            int counter = 0;
            var userC = 0;
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
            int existCounter = 0;
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
                catch (Exception ex)
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
      //  [HttpGet("FillingSatisfaction")]
        //public async Task<IActionResult> FillingSatisfaction( [FromQuery(Name = "dialogueId")] Guid? dialogueId)
        //{
        //    FillingSatisfactionService.CalculationConfig _config = new FillingSatisfactionService.CalculationConfig();
        //    Calculations _calculations = new Calculations(_context, _config);
        //    var begTime = new DateTime(2019, 09, 21);
        //    var endTime = new DateTime(2019, 09, 24);

        //    try
        //    {
        //        _log.Info("Function started");
        //        var dialogue = _context.Dialogues
        //            .Include(p => p.DialogueFrame)
        //            .Include(p => p.DialogueAudio)
        //            .Include(p => p.DialogueSpeech)
        //            .Include(p => p.DialogueInterval)
        //            .Include(p => p.DialogueClientProfile)
        //            .FirstOrDefault(p => p.DialogueId == dialogueId);
        //        var dialogueFrame = dialogue.DialogueFrame;
        //        var dialogueAudio = dialogue.DialogueAudio.FirstOrDefault();
        //        var positiveTextTone = dialogue.DialogueSpeech.FirstOrDefault() == null ? null : dialogue.DialogueSpeech.FirstOrDefault().PositiveShare;
        //        var dialogueInterval = dialogue.DialogueInterval;
        //        var dialogueClientProfile = dialogue.DialogueClientProfile;

        //        // var meetingExpectationsByNN =
        //        // _calculations.TotalScoreInsideCalculate(dialogueFrame, dialogueAudio,
        //        // positiveTextTone);
        //        var meetingExpectationsByNN = _calculations.TotalScoreCalculate(dialogue);


        //        Double? begMoodByNN = 0;
        //        Double? endMoodByNN = 0;
        //        Double nNWeight = 0;

        //        if (dialogueFrame.Any())
        //        {
        //            var framesCountPeriod = Math.Min(10, dialogueFrame.Count() / 3);
        //            var intervalCountPeriod = Math.Min(10, dialogueInterval.Count() / 3);

        //            //BorderMoodCalculateList
        //            begMoodByNN = _calculations
        //               .BorderMoodCalculateList(dialogueFrame.Take(framesCountPeriod).ToList(),
        //                    dialogueInterval.Take(intervalCountPeriod).ToList(),
        //                    meetingExpectationsByNN);
        //            endMoodByNN = _calculations
        //               .BorderMoodCalculateList(
        //                    dialogueFrame
        //                       .Skip(Math.Max(0, dialogueFrame.Count() - framesCountPeriod)).ToList(),
        //                    dialogueInterval
        //                       .Skip(Math.Max(0, dialogueInterval.Count() - intervalCountPeriod))
        //                       .ToList(),
        //                    meetingExpectationsByNN);

        //            nNWeight = 0.2;
        //        }
        //        else
        //        {
        //            begMoodByNN = null;
        //            endMoodByNN = null;
        //            nNWeight = 0;
        //        }

        //        DialogueClientSatisfaction satisfactionScore = _context.DialogueClientSatisfactions
        //                    .FirstOrDefault(p => p.DialogueId == dialogueId);

        //        double clientWeight = 0, employeeWeight = 0, teacherWeight = 0;
        //        double clientTotalScore = 0, employeeTotalScore = 0, teacherTotalScore = 0;
        //        double employeeBegScore = 0, teacherBegScore = 0;
        //        double employeeEndScore = 0, teacherEndScore = 0;
        //        if (satisfactionScore != null)
        //        {
        //            satisfactionScore.MeetingExpectationsByClient = _calculations.MeetingExpectationsByClientCalculate(dialogue);
        //            clientTotalScore = Convert.ToDouble(satisfactionScore.MeetingExpectationsByClient);
        //            clientWeight = Convert.ToDouble(0.2);
        //            if (satisfactionScore.MeetingExpectationsByEmpoyee != null)
        //            {
        //                employeeTotalScore = Convert.ToDouble(satisfactionScore.MeetingExpectationsByEmpoyee);
        //                employeeBegScore = Convert.ToDouble(satisfactionScore.BegMoodByEmpoyee);
        //                employeeEndScore = Convert.ToDouble(satisfactionScore.EndMoodByEmpoyee);
        //                employeeWeight = Convert.ToDouble(0.2);
        //            }

        //            if (satisfactionScore.MeetingExpectationsByTeacher != null)
        //            {
        //                teacherTotalScore = Convert.ToDouble(satisfactionScore.MeetingExpectationsByTeacher);
        //                teacherBegScore = Convert.ToDouble(satisfactionScore.BegMoodByTeacher);
        //                teacherEndScore = Convert.ToDouble(satisfactionScore.EndMoodByTeacher);
        //                teacherWeight = Convert.ToDouble(0.2);
        //            }
        //        }

        //        var sumWeight = nNWeight + clientWeight + employeeWeight + teacherWeight;
        //        var sumWeightExceptClient = nNWeight + employeeWeight + teacherWeight;

        //        Double? meetingExpectationsTotal = null;
        //        if (sumWeight != 0)
        //        {
        //            meetingExpectationsTotal =
        //                (clientWeight * clientTotalScore + nNWeight * meetingExpectationsByNN +
        //                 employeeWeight * employeeTotalScore + teacherWeight * teacherTotalScore) / sumWeight;
        //        }

        //        Double? begMoodTotal = null, endMoodTotal = null;
        //        if (sumWeightExceptClient != 0)
        //        {
        //            begMoodTotal =
        //                (nNWeight * begMoodByNN + employeeBegScore * employeeWeight + teacherBegScore * teacherWeight) /
        //                sumWeightExceptClient;
        //            endMoodTotal =
        //                (nNWeight * endMoodByNN + employeeEndScore * employeeWeight + teacherEndScore * teacherWeight) /
        //                sumWeightExceptClient;
        //        }

        //        var random = new Random();
        //        if (satisfactionScore == null)
        //        {
        //            satisfactionScore = new DialogueClientSatisfaction
        //            {
        //                DialogueClientSatisfactionId = Guid.NewGuid(),
        //                DialogueId = dialogueId
        //            };
        //            _context.DialogueClientSatisfactions.Add(satisfactionScore);
        //        }
        //        satisfactionScore.MeetingExpectationsTotal = Math.Max((double)meetingExpectationsTotal, 35);
        //        satisfactionScore.MeetingExpectationsByNN = Math.Max((double)meetingExpectationsByNN, 35);
        //        satisfactionScore.BegMoodTotal = Math.Max((double)begMoodTotal, 35);
        //        satisfactionScore.BegMoodByNN = Math.Max((double)begMoodByNN, 35);
        //        satisfactionScore.EndMoodTotal = Math.Max((double)endMoodTotal, 35);
        //        satisfactionScore.EndMoodByNN = Math.Max((double)endMoodByNN, 35);
        //        satisfactionScore.MeetingExpectationsByClient = _calculations.MeetingExpectationsByClientCalculate(dialogue);
        //        satisfactionScore.Age = dialogueClientProfile != null ? dialogueClientProfile.Average(x => x.Age) : null;
        //        satisfactionScore.Gender = dialogueClientProfile != null ?
        //            dialogueClientProfile.Count(x => x.Gender == "male") > dialogueClientProfile.Count(x => x.Gender == "female") ?
        //            "male" : "female" : null;
        //        _log.Info($"Total mood is --- {satisfactionScore.MeetingExpectationsTotal}");


        //        _context.SaveChanges();             
        //        _log.Info("Function filling satisfaction ended.");
        //    }
        //    catch (Exception e)
        //    {
        //        _log.Fatal($"exception occured {e}");
        //        throw;
        //    }

        //    return Ok();
        //}
        [HttpGet("CheckSessions")]
        public async Task<IActionResult> CheckSessions()
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

        [HttpGet("{id}")]
        public async Task<ActionResult> Test5()
        {
            string id = (string)RouteData.Values["id"];
            var dialogs = _repository.GetWithInclude<Dialogue>(
                d => d.StatusId == 3 && d.LanguageId==2);

            return Ok(dialogs.Count());
        }

        [HttpGet("phrase")]
        public IActionResult Phrase()
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