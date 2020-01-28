using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;
using HBData.Models;
using HBData.Models.AccountViewModels;
using UserOperations.Services;
using UserOperations.Models;
using UserOperations.AccountModels;
using HBData;
using UserOperations.Models;
using System.Data.SqlClient;
using System.Threading;


using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using HBLib.Utils;
using System.IO;

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


        public HelpController(
            IConfiguration config,
            ILoginService loginService,
            RecordsContext context,
            SftpClient sftpClient,
            MailSender mailSender
            )
        {
            _config = config;
            _loginService = loginService;
            _context = context;
            _sftpClient = sftpClient;
            _mailSender = mailSender;
        }

        [HttpGet("Help1")]
        public async Task<IActionResult> Help1()
        {
            string res1 = await _mailSender.TestReadFile1();
            return Ok(res1);
        }

        [AllowAnonymous]
        [HttpGet("GetAllData")]
        public async Task<JsonResult> GetAllDataAsync()
        {
            try
            {  
                var dialogues = _context.Dialogues
                    .Include(p => p.DialogueAudio)
                    .Include(p => p.DialogueClientProfile)
                    .Include(p => p.DialogueClientSatisfaction)
                    .Include(p => p.DialogueFrame)
                    .Include(p => p.DialogueInterval)
                    .Include(p => p.DialoguePhrase)
                    .Include(p => p.DialogueSpeech)
                    .Include(p => p.DialogueVisual)
                    .Include(p => p.DialogueWord)
                    .Where(p => p.StatusId == 3)
                    .Where(p => p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsByTeacher != null &&  p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsByTeacher != 0)
                    .ToList();
                System.Console.WriteLine(dialogues.Count());
                byte[] byteArray = Encoding.ASCII.GetBytes( JsonConvert.SerializeObject(dialogues) );
                MemoryStream memoryStream = new MemoryStream( byteArray );
                var fileName = "dump_20191129.txt";
                await _sftpClient.UploadAsMemoryStreamAsync(memoryStream, "test/", fileName);
                return Json(dialogues);

            }
            catch (Exception e)
            {
                throw e;
            }
        }
     

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

        [HttpGet("newtestAsync")]
        public async Task<IActionResult> newtestAsync([FromQuery] string type)
        {
            List<TestClassPhrase> items;
            using (StreamReader r = new StreamReader($"phrase_{type}.json"))
            {
                string json = r.ReadToEnd();
                items = JsonConvert.DeserializeObject<List<TestClassPhrase>>(json);
            }
            var phraseTypeId = _context.PhraseTypes.Where(p => p.PhraseTypeText.ToLower() == type.ToLower()).First().PhraseTypeId;
            var phrases = new List<Phrase>();
            var phraseCompanies = new List<PhraseCompany>();

            foreach (var item in items)
            {
                var phrase = new Phrase();
                phrase.Accurancy = 1;
                phrase.IsClient = true;
                phrase.IsTemplate = true;
                phrase.LanguageId = 1;
                phrase.PhraseId = Guid.NewGuid();
                phrase.PhraseText = item.PhraseText;
                phrase.PhraseTypeId = phraseTypeId;
                phrase.WordsSpace = 1;
                phrases.Add(phrase);
            }

            var companys = _context.Companys.Select(p => p.CompanyId).ToList();
            foreach( var comp in companys)
            {
                foreach (var phrase in phrases)
                {
                    var compPhrase = new PhraseCompany();
                    compPhrase.CompanyId = comp;
                    compPhrase.PhraseId = phrase.PhraseId;
                    compPhrase.PhraseCompanyId = Guid.NewGuid(); 
                    phraseCompanies.Add(compPhrase);
                }
            }
            _context.Phrases.AddRange(phrases);
            _context.PhraseCompanys.AddRange(phraseCompanies);
            _context.SaveChanges();

            return Ok(items);
            // return Ok();
        }
        // [HttpGet("AddContent")]
        // public IActionResult addcontent([FromBody] string companyName)
        // {
        //     var companyId = _context.Companys.First(p => p.CompanyName == companyName).CompanyId;
        //     var companys = _context.Companys.Where(p => p.CompanyName.StartsWith("Sberbank")).ToList();
        //     companys 
        //     var content = _context.Contents.Where(p => p.CompanyId == companyId).ToList();

        //     return Ok();
        // }

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

       
        
        [HttpPost("test")]
        public IActionResult test([FromBody]DateTime dateTime)
        {
           
            var dialogues = _context.Dialogues
                .Include(p => p.DialogueWord)
                .Where(p => !p.DialogueWord.Any() && p.BegTime >= dateTime)
                .Select(p => p.DialogueId)
                .ToList();
            
            foreach (var dialogueId in dialogues)
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://10.32.10.115/user/AudioAnalyze/audio-analyze");
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                   var dict = new Dictionary<string, string>();
                   dict["Path"] = $"dialogueaudios/{dialogueId}.wav";
                    streamWriter.Write(JsonConvert.SerializeObject(dict));
                }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    System.Console.WriteLine("Result" + result);
                }

                Thread.Sleep(300);
            }
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