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
        }

        [HttpGet("PasswordHistoryTest")]
        public async Task<IActionResult> PasswordHistoryTest()
        {
            Guid userId = new Guid("d1918ef5-fad4-4678-b48f-87908093520c");
            _loginService.SavePasswordHistory(userId, "4I95gSV7j/vb2MB9PMtFbSvR1ShYlALSKB5u78u+bDd=");
            return Ok(_context.PasswordHistorys.ToList());
        }


        [HttpGet("GetWords")]
        public async Task<IActionResult> GetWords()
        {
            var words = _context.DialogueWords.Take(10);//.ToList();
            foreach (DialogueWord w in words)
            {
                if(w.Words.Contains("\"NULL\""))
                {
                w.Words = w.Words.Replace("\"NULL\"", "null");
                _context.SaveChanges();
                }
            }

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


        //[HttpGet("UserRegister")]
        //         public async Task<IActionResult> UserRegister()
        //         {
        //             List<UserRegister2> messages = new List<UserRegister2>{
        // //new UserRegister2{Id="0450a19a-3679-43c7-8822-5f70e9a142ea",FullName="Alina Ochirova",Email="ochirova@heedbook.com",Password="password_123",CompanyName="Heedbook",LanguageId=2,CountryId="BE6A6509-7C9E-4D63-B787-5725BBBB2F26",CompanyIndustryId="A49EC5F5-DCB7-478B-8058-692AE8D26946",CorporationId="52402355-ef7c-41bd-b28e-4234a889c3ba", StatusId=3,CompanyId="5F4BE5EE-4342-42F9-A8D8-658CAAB8415A",CreationDate="2017-11-29 15:12:46.2717119",UserName="ochirova@heedbook.com"},
        //               };

        //             foreach (var message in messages)
        //             {

        //                 // if (_context.Companys.Where(x => x.CompanyName == message.CompanyName).Any() || _context.ApplicationUsers.Where(x => x.NormalizedEmail == message.Email.ToUpper()).Any())
        //                 //     return BadRequest("Company name or user email not unique");
        //                 try
        //                 {
        //                     var companyId = Guid.Parse(message.CompanyId);
        //                     Console.WriteLine("1--try---" + companyId);
        //                     if (!_context.Companys.Any(x => x.CompanyId == companyId))
        //                     {
        //                         var company = new Company
        //                         {
        //                             CompanyId = companyId,
        //                             CompanyIndustryId = message.CompanyIndustryId != null ?(Guid?) Guid.Parse(message.CompanyIndustryId) : null,
        //                             CompanyName = message.CompanyName,
        //                             LanguageId = message.LanguageId,
        //                             CreationDate = DateTime.Parse(message.CreationDate),
        //                             CountryId = message.CountryId != null ? (Guid?)Guid.Parse(message.CountryId) : null,
        //                             CorporationId = message.CorporationId != null ?(Guid?) Guid.Parse(message.CorporationId) : null,
        //                             StatusId = message.StatusId//---inactive
        //                         };
        //                         await _context.Companys.AddAsync(company);
        //                       //   await _context.SaveChangesAsync();
        //                         Console.WriteLine("2--added---" + companyId);
        //                     }
        //                     if( _context.ApplicationUsers.Where(x => x.NormalizedEmail == message.Email.ToUpper()).Any())
        //                     {
        //                        var m =  message.Email.Split('@');//+="2";
        //                        message.Email = m[0]+"2@"+m[1];                       
        //                     }
        //                      if( _context.ApplicationUsers.Where(x => x.UserName == message.UserName.ToUpper()).Any())
        //                     {
        //                        var m =  message.UserName.Split('@');//+="2";
        //                        if(m.Length>1)
        //                        message.UserName = m[0]+"2@"+m[1];  
        //                        else
        //                               message.UserName+="2";               
        //                     }


        //                     var user = new ApplicationUser
        //                     {
        //                         UserName = message.UserName,
        //                         NormalizedUserName = message.Email.ToUpper(),
        //                         Email = message.Email,
        //                         NormalizedEmail = message.Email.ToUpper(),
        //                         Id = Guid.Parse(message.Id),
        //                         CompanyId = companyId,
        //                         CreationDate = DateTime.Parse(message.CreationDate),
        //                         FullName = message.FullName,
        //                         PasswordHash = _loginService.GeneratePasswordHash(message.Password),
        //                         StatusId = message.StatusId
        //                     };
        //                     await _context.AddAsync(user);
        //                  //    await _context.SaveChangesAsync();
        //                     Console.WriteLine("3--user---" + user.Id);

        //                     var userRole = new ApplicationUserRole()
        //                     {
        //                         UserId = user.Id,
        //                         RoleId = _context.Roles.First(p => p.Name == "Manager").Id //Manager role
        //                     };
        //                     await _context.ApplicationUserRoles.AddAsync(userRole);
        //                    //  await _context.SaveChangesAsync();

        //                     if (_context.Tariffs.Where(item => item.CompanyId == companyId).ToList().Count() == 0)
        //                     {
        //                         var tariff = new Tariff
        //                         {
        //                             TariffId = Guid.NewGuid(),
        //                             TotalRate = 0,
        //                             CompanyId = companyId,
        //                             CreationDate = DateTime.UtcNow,
        //                             CustomerKey = "",
        //                             EmployeeNo = 2,
        //                             ExpirationDate = DateTime.UtcNow.AddDays(5),
        //                             isMonthly = false,
        //                             Rebillid = "",
        //                             StatusId = _context.Statuss.FirstOrDefault(p => p.StatusName == "Trial").StatusId//---Trial
        //                         };

        //                         var transaction = new Transaction
        //                         {
        //                             TransactionId = Guid.NewGuid(),
        //                             Amount = 0,
        //                             OrderId = "",
        //                             PaymentId = "",
        //                             TariffId = tariff.TariffId,
        //                             StatusId = _context.Statuss.FirstOrDefault(p => p.StatusName == "Finished").StatusId,//---finished
        //                             PaymentDate = DateTime.UtcNow,
        //                             TransactionComment = "TRIAL TARIFF;FAKE TRANSACTION"
        //                         };
        //                         //  company.StatusId = message.StatusId;

        //                         await _context.Tariffs.AddAsync(tariff);
        //                         await _context.Transactions.AddAsync(transaction);
        //                         // var ids = _context.ApplicationUsers.Where(p => p.Id == user.Id).ToList();

        //                     }
        //                     else
        //                     {

        //                     }
        //                     await _context.SaveChangesAsync();

        //                     Console.WriteLine("4--complite---");
        //                 }
        //                 catch (Exception e)
        //                 {
        //                     Console.WriteLine(e.InnerException.Message);
        //                     //  return BadRequest(e.ToString());
        //                 }
        //             }
        //                         _context.Dispose();

        //             return Ok("Registred");

        //         }

        #region DatabaseFilling

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
        #endregion
    }
}