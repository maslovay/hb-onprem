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
using UserOperations.AccountModels;
using HBData;

///REMOVE IT
using System.Data.SqlClient;


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

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HelpController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _config;
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
        private readonly SftpClient _sftpClient;
        private readonly int activeStatus;


        public HelpController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration config,
            ILoginService loginService,
            RecordsContext context,
              SftpClient sftpClient
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
            _loginService = loginService;
            _context = context;
            _sftpClient = sftpClient;
            activeStatus = _context.Statuss.FirstOrDefault(p => p.StatusName == "Active").StatusId;
        }

        [HttpGet("GetWords")]
        public async Task<IActionResult> GetWords()
        {         

            // string connectionString = @"Server=tcp:hbrestoreserver.database.windows.net,1433;Initial Catalog=hbmssqldb;Persist Security Info=False;User ID=test_user;Password=password_123;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
            // SqlConnection connection = new SqlConnection(connectionString);
            // int i = 0;
            // Guid id = default(Guid);
            // try
            // {
            //     string sqlExpression = "SELECT * FROM ResultTable";
            //     // Открываем подключение
            //     //    using (SqlConnection connection = new SqlConnection(connectionString))
            //     {

            //         connection.Open();
            //         SqlCommand command = new SqlCommand(sqlExpression, connection);
            //         SqlDataReader reader = command.ExecuteReader();

            //         if (reader.HasRows) // если есть данные
            //         {
            //             // выводим названия столбцов

            //             while (reader.Read()) // построчно считываем данные
            //             {


            //                 id = (Guid)reader.GetValue(0);
            //                 object word = reader.GetValue(1);
            //                 object isClient = reader.GetValue(2);
            //                 try
            //                 {
            //                         DialogueWord newWord = new DialogueWord();
            //                         newWord.DialogueWordId = Guid.NewGuid();
            //                         newWord.DialogueId = id;
            //                         newWord.Words = "[" + word.ToString()+"]";
            //                         newWord.IsClient = (bool)isClient;
            //                         _context.Add(newWord);
            //                     _context.SaveChanges();
            //                 }
            //                 catch
            //                 {
            //                     Console.WriteLine("{0} \t{1} \t{2}", "error  ---- ", i++, id);
            //                 }
            //                 Console.WriteLine("----------------------------------------------------------");
            //                 Console.WriteLine("{0} \t{1} \t{2}", "saved ---- ", i++, id);
            //             }
            //         }

            //         reader.Close();
            //     }

            // }
            // catch (SqlException ex)
            // {
            //  //   Console.WriteLine(ex.Message);
            //     Console.WriteLine("----------------------------------------------------------");
            //     Console.WriteLine("{0} \t{1} \t{2}", "error  ---- ", i++, id);
            // }
            // finally
            // {
            //     // закрываем подключение
            //     connection.Close();
            //     Console.WriteLine("Подключение закрыто...");
            // }

            return Ok("I've done");
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

        [HttpGet("test123")]
        public IActionResult test123([FromQuery]Guid? dialogueId)
        {
                 var dialogue = _context.Dialogues
                    .Include(p => p.DialogueAudio)
                    .Include(p => p.DialogueClientProfile)
                    .Include(p => p.DialogueClientSatisfaction)
                    .Include(p => p.DialogueFrame)
                    .Include(p => p.DialogueInterval)
                    .Include(p => p.DialoguePhrase)
                    .Include(p => p.DialoguePhraseCount)
                    .Include(p => p.DialogueSpeech)
                    .Include(p => p.DialogueVisual)
                    .Include(p => p.DialogueWord)
                    .Include(p => p.ApplicationUser)
                    .Include(p => p.DialogueHint)
                    .Where(p => p.StatusId == activeStatus && p.DialogueId == dialogueId)
                    .FirstOrDefault();
                    return Ok(dialogue);
            try
            {
                // var dialogue = _context.Dialogues
                //  .Include(p => p.DialogueAudio)
                //     .Include(p => p.DialogueClientProfile)
                //     .Include(p => p.DialogueClientSatisfaction)
                //     .Include(p => p.DialogueFrame)
                //     .Include(p => p.DialogueInterval)
                //     .Include(p => p.DialoguePhrase)
                //     .Include(p => p.DialoguePhraseCount)
                //     .Include(p => p.DialogueSpeech)
                //     .Include(p => p.DialogueVisual)
                //     .Include(p => p.DialogueWord)
                //     .Include(p => p.ApplicationUser)
                //     .Include(p => p.DialogueHint)
                // .Where(p => p.DialogueId.ToString() == "a54b3bc8-d948-4f28-99fe-98f232f65ef4").ToList();
                // var frames  = _context.FileFrames.Where(p => p.StatusNNId == 7).ToList();
                // frames.ForEach(p => p.FaceId = null);
                // frames.ForEach(p => p.StatusNNId = 6);
                // _context.SaveChanges();

                // var dialogue = _context.Dialogues
                //     .Include(p => p.DialogueAudio)
                //     .Include(p => p.DialogueClientProfile)
                //     .Include(p => p.DialogueClientSatisfaction)
                //     .Include(p => p.DialogueFrame)
                //     .Include(p => p.DialogueInterval)
                //     .Include(p => p.DialoguePhrase)
                //     .Include(p => p.DialoguePhraseCount)
                //     .Include(p => p.DialogueSpeech)
                //     .Include(p => p.DialogueVisual)
                //     .Include(p => p.DialogueWord)
                //     .Include(p => p.ApplicationUser)
                //     .Include(p => p.DialogueHint)
                //     .Where(p => p.DialogueId == dialogueId).ToList();
                var fileFrame = _context.FileVideos.Where(p => p.FileVideoId.ToString() == "b52238b9-fae2-43c7-9a6d-fdea1fe00cd1").ToList();
                return Ok(fileFrame);
            }   
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }
    }
}