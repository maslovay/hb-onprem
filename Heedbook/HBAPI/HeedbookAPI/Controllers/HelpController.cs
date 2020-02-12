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
using Old.Models;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HelpController : Controller
    {
    //    private readonly IConfiguration _config;
    //    private readonly LoginService _loginService;
        private readonly RecordsContext _context;
    //    private readonly SftpClient _sftpClient;
    //    private readonly MailSender _mailSender;
    //    private readonly RequestFilters _requestFilters;
    //    private readonly SftpSettings _sftpSettings;
    //    private readonly DBOperations _dbOperation;
    //    private readonly IGenericRepository _repository;


        public HelpController(
            //IConfiguration config,
            //LoginService loginService,
            RecordsContext context
            //SftpClient sftpClient,
            //MailSender mailSender,
            //RequestFilters requestFilters,
            //SftpSettings sftpSettings,
            //DBOperations dBOperations,
            //IGenericRepository repository
            )
        {
            //_config = config;
            //_loginService = loginService;
            _context = context;
            //_sftpClient = sftpClient;
            //_mailSender = mailSender;
            //_requestFilters = requestFilters;
            //_sftpSettings = sftpSettings;
            //_dbOperation = dBOperations;
            //_repository = repository;
        }

        [HttpGet("CopyDataFromDB")]
        public async Task<IActionResult> CopyDataFromDB()
        {
            var connectionString = "User ID=test_user;Password=test_password;Host=40.69.85.202;Port=5432;Database=test_db;Pooling=true;Timeout=120;CommandTimeout=0;";

            //var connectionString = "User ID=heedbook_user;Password=Oleg&AnnaRulyat_1975;Host=40.69.85.202;Port=5432;Database=heedbook_db;Pooling=true;Timeout=120;CommandTimeout=0;";
            DbContextOptionsBuilder<OldRecordsContext> dbContextOptionsBuilder = new DbContextOptionsBuilder<OldRecordsContext>();
            dbContextOptionsBuilder.UseNpgsql(connectionString,
                   dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(UserOperations)));
            var oldContext = new OldRecordsContext(dbContextOptionsBuilder.Options);

            var oldAlerts = oldContext.Alerts.ToList();
            var newAlertsId = _context.Alerts.Select(x => x.AlertId).ToList();
            var addAlert = oldAlerts.Where(x => !newAlertsId.Contains(x.AlertId));
            _context.Add(addAlert);
            _context.SaveChanges();

            //_context.Add(contentInBackup);
            //_context.SaveChanges();
            return Ok();
        }

        [HttpGet("Help3")]
        public async Task<IActionResult> Help3()
        {
            //var connectionString = "User ID = postgres; Password = annushka123; Host = 127.0.0.1; Port = 5432; Database = onprem_backup; Pooling = true; Timeout = 120; CommandTimeout = 0";

            var connectionString = "User ID=heedbook_user;Password=Oleg&AnnaRulyat_1975;Host=40.69.85.202;Port=5432;Database=heedbook_db;Pooling=true;Timeout=120;CommandTimeout=0;";
            DbContextOptionsBuilder<RecordsContext> dbContextOptionsBuilder = new DbContextOptionsBuilder<RecordsContext>();
            dbContextOptionsBuilder.UseNpgsql(connectionString,
                   dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(UserOperations)));
            var localContext = new RecordsContext(dbContextOptionsBuilder.Options);
            Guid contentPrototypeId = new Guid("07565966-7db2-49a7-87d4-1345c729a6cb");
            //var c = localContext.Contents.ToList();
            var contentInBackup = localContext.Contents.FirstOrDefault(x => x.ContentId == contentPrototypeId);

            contentInBackup.CreationDate = DateTime.Now;
            contentInBackup.UpdateDate = DateTime.Now;
            contentInBackup.CompanyId = null;
            //_context.Add(contentInBackup);
            //_context.SaveChanges();
            return Ok();
        }

        [HttpGet("Help4")]
        public async Task<IActionResult> MoveDialogues()
        {
            //var connectionString = "User ID = postgres; Password = annushka123; Host = 127.0.0.1; Port = 5432; Database = onprem_backup; Pooling = true; Timeout = 120; CommandTimeout = 0";

            var connectionString = "User ID=heedbook_user;Password=Oleg&AnnaRulyat_1975;Host=40.69.85.202;Port=5432;Database=heedbook_db;Pooling=true;Timeout=120;CommandTimeout=0;";
            DbContextOptionsBuilder<RecordsContext> dbContextOptionsBuilder = new DbContextOptionsBuilder<RecordsContext>();
            dbContextOptionsBuilder.UseNpgsql(connectionString,
                   dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(UserOperations)));
            var localContext = new RecordsContext(dbContextOptionsBuilder.Options);

            try
            {
                var dialogueIds = new string[]
                {
                    "31db5a99-1966-45f9-8675-d2197210f340",
                    "cd08dbd3-6819-4322-99bf-80199f141ab8",
                    "c8c4de56-b032-4f02-8b8a-6c650e6ba5bd",
                    "5052b6b9-d6d7-4be4-83a0-74ade80d16da",
                    "ce894397-8933-4a90-ad25-335c1752d807",
                    "ca6aadff-e00d-4ba0-aae1-08ea0bfeee59",
                    "c6fc6a8a-8198-4154-b97e-f1596af2b100",
                    "57d811e4-a9bc-4ec2-ba74-3244162d4580",
                    "58dfb7e2-4fa3-4fca-b8a4-31265170c95c",
                    "30419a61-e5b5-4a82-ac3a-c8c841ce2dd3",
                    "477bb5ca-fd81-44cf-a981-bbdbf906b303"
                };

                var phrases = localContext.Phrases.ToList();
                //_context.AddRange(phrases);
                //_context.SaveChanges();
                var dialogueAudios = localContext.DialoguePhrases.Where(x => dialogueIds.Contains(x.DialogueId.ToString())).ToList();
                var dialogueAudios1 = localContext.DialogueSpeechs.Where(x => dialogueIds.Contains(x.DialogueId.ToString())).ToList();
                var dialogueAudios2 = localContext.DialogueVisuals.Where(x => dialogueIds.Contains(x.DialogueId.ToString())).ToList();
                var dialogueAudios3 = localContext.DialogueWords.Where(x => dialogueIds.Contains(x.DialogueId.ToString())).ToList();


                //_context.AddRange(dialogueAudios);
                //_context.AddRange(dialogueAudios1);
                //_context.AddRange(dialogueAudios2);
                //_context.AddRange(dialogueAudios3);
                //_context.SaveChanges();
                return Ok();
            }
            catch(Exception ex)
            {
                var e = ex.Message;
                return BadRequest(ex.Message);
            }
        }


        [HttpGet("SalesStageAdd")]
        public IActionResult SalesStageAdd()
        {

            List<Temp> arr = new List<Temp> {
                 new Temp { phrase = "���������� ������	", id = 3},
                 new Temp { phrase = "������������ ������	", id = 3}
            };
            foreach (var item in arr)
            {
            try
            {
                    Guid phraseId = _context.Phrases.FirstOrDefault(x => x.PhraseText == item.phrase.TrimEnd(' ').TrimEnd('\t')).PhraseId;
                    SalesStagePhrase ssphr = _context.SalesStagePhrases.Where(x => x.PhraseId == phraseId).FirstOrDefault();
                    if (ssphr == null)
                    {
                        SalesStagePhrase ssph = new SalesStagePhrase
                        {
                            SalesStagePhraseId = Guid.NewGuid(),
                            PhraseId = _context.Phrases.FirstOrDefault(x => x.PhraseText == item.phrase.TrimEnd(' ').TrimEnd('\t')).PhraseId,
                            SalesStageId = _context.SalesStages.FirstOrDefault(x => x.SequenceNumber == item.id).SalesStageId,
                        };
                        _context.SalesStagePhrases.Add(ssph);
                        _context.SaveChanges();
                    }
                }
            catch{}
            }
            return Ok();
        }

        public class Temp
        {
            public int id;
            public string phrase;
        }
    }
}