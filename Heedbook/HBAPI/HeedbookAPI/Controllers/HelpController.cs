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


        public HelpController(
            IConfiguration config,
            ILoginService loginService,
            RecordsContext context,
            SftpClient sftpClient,
            MailSender mailSender,
            RequestFilters requestFilters
            )
        {
            _config = config;
            _loginService = loginService;
            _context = context;
            _sftpClient = sftpClient;
            _mailSender = mailSender;
            _requestFilters = requestFilters;
        }

     

        [HttpGet("GetBenchmarks")]
        public async Task<IActionResult> GetBenchmarks([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end,
                                                        [FromQuery(Name = "userId")] Guid? userId)
        {
            var industryId = _context.ApplicationUsers.Include(x => x.Company).FirstOrDefault(x => x.Id == userId).Company.CompanyIndustryId;
            var begTime = _requestFilters.GetBegDate(beg);
            var endTime = _requestFilters.GetEndDate(end);
            var indexes = _context.Benchmarks.Where(x => x.Day >= begTime && x.Day <= endTime
                                            && (x.IndustryId == industryId || x.IndustryId == null))
                                            .Join(_context.BenchmarkNames,
                                                bench => bench.BenchmarkNameId,
                                                names => names.Id,
                                                (bench, names) => new { names.Name, bench.Value })
                                            .ToList();

            var result = indexes.Where(x => x.Name.Contains("Avg"))
                                            .GroupBy(x => x.Name)
                                            .ToDictionary(gr => gr.Key, v => v.Average(p => p.Value))
                           .Union(
                           indexes.Where(x => x.Name.Contains("Benchmark"))
                                            .GroupBy(x => x.Name)
                                            .ToDictionary(gr => gr.Key, v => v.Max(p => p.Value))
                            )
                            .ToDictionary(gr => gr.Key, v => v.Value);
            return Ok(result);
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
}