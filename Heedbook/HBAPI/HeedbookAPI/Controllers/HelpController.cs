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
using HBData.Repository;
using System.Data;
using System.Reflection;
using System.Data.SqlClient;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HelpController : Controller
    {
        private readonly IConfiguration _config;
        private readonly CompanyService _compService;
        private readonly RecordsContext _context;
        //    private readonly SftpClient _sftpClient;
        //    private readonly MailSender _mailSender;
        private readonly RequestFilters _requestFilters;
        //    private readonly SftpSettings _sftpSettings;
        //    private readonly DBOperations _dbOperation;
        //    private readonly IGenericRepository _repository;
      //  private readonly DescriptorCalculations _calc;


        public HelpController(
            CompanyService compService,
            IConfiguration config,
            RecordsContext context,
          //   DescriptorCalculations calc
            //SftpClient sftpClient,
            //MailSender mailSender,
            RequestFilters requestFilters
            //SftpSettings sftpSettings,
            //DBOperations dBOperations,
            //IGenericRepository repository
            )
        {
            _compService = compService;
            _config = config;
            _context = context;
         //   _calc = calc;
            //_sftpClient = sftpClient;
            //_mailSender = mailSender;
            _requestFilters = requestFilters;
            //_sftpSettings = sftpSettings;
            //_dbOperation = dBOperations;
            //_repository = repository;
        }

        [HttpGet("DevicesCreate")]
        public async Task<IActionResult> DevicesCreate(int skip, int take)
        {
            var userid = _context.SlideShowSessions.Where(x => x.DeviceId == Guid.Empty).OrderByDescending(x => x.BegTime).Skip(skip).Take(take).ToList();

            var devices = _context.Devices.Include(x => x.Company.ApplicationUser)
                .Select(x => new { x.DeviceId, applicationUserIds = x.Company.ApplicationUser.Select(p => p.Id).ToList() }).ToList();

            foreach (var item in userid)
            {
                item.DeviceId = devices.Where(x => x.applicationUserIds.Contains((Guid)item.ApplicationUserId)).FirstOrDefault().DeviceId;

            }
            _context.SaveChanges();

            return Ok();
        }

        [HttpGet("SalesStageCreate")]
        public async Task<IActionResult> SalesStageCreate()
        {
            var companyIds = _context.Companys.Where(x => x.CorporationId == null && !x.SalesStagePhrases.Any()).Select(x => x.CompanyId).ToList();
            var ssPhrases = _context.SalesStagePhrases.Where(x => x.CompanyId == null && x.CorporationId == null).ToList();
            try
            {
                foreach (var companyId in companyIds)
                {
                    if (!_context.SalesStagePhrases.Any(x => x.CompanyId == companyId))
                    {
                        var ssPh = ssPhrases.Select(x => new SalesStagePhrase
                        {
                            CompanyId = companyId,
                            PhraseId = x.PhraseId,
                            SalesStageId = x.SalesStageId//,
                                                         //  SalesStagePhraseId = Guid.NewGuid()
                        }).ToList();
                        _context.AddRange(ssPh);
                        _context.SaveChanges();
                    }

                }

                var corporationIds = _context.Companys.Where(x => x.CorporationId != null && !x.SalesStagePhrases.Any()).Select(x => x.CorporationId).ToList();
                foreach (var corporationId in corporationIds)
                {
                    if (!_context.SalesStagePhrases.Any(x => x.CorporationId == corporationId))
                    {
                        var ssPh = ssPhrases.Select(x => new SalesStagePhrase
                        {
                            CorporationId = corporationId,
                            PhraseId = x.PhraseId,
                            SalesStageId = x.SalesStageId//,
                                                         //  SalesStagePhraseId = Guid.NewGuid()
                        }).ToList();
                        _context.AddRange(ssPh);
                        _context.SaveChanges();
                    }

                }
            }
            catch (Exception ex) { var m = ex.Message; }

            return Ok();
        }

        [HttpGet("PhraseClear")]
        public async Task<IActionResult> PhraseClear()
        {
            var phrases = _context.Phrases.Include(x => x.PhraseCompanys).Include(x => x.DialoguePhrases).GroupBy(x => x.PhraseText).ToList();

            foreach (var group in phrases)
            {
                if (group.Count() > 1)
                {
                    var templ = group.Where(x => x.IsTemplate == true).FirstOrDefault();
                    if (templ != null)
                    {
                        var phs = group.Where(x => x.IsTemplate == false).ToList();
                        foreach (var ph in phs)
                        {
                            var phraseComp = _context.PhraseCompanys.Where(x => x.PhraseId == ph.PhraseId).ToList();
                            var dialogPhrases = _context.DialoguePhrases.Where(x => x.PhraseId == ph.PhraseId).ToList();
                            foreach (var phCom in phraseComp)
                            {
                                phCom.PhraseId = templ.PhraseId;
                            }

                            foreach (var phD in dialogPhrases)
                            {
                                phD.PhraseId = templ.PhraseId;
                            }
                        }
                        _context.RemoveRange(phs);
                        _context.SaveChanges();
                    }
                    else
                    {
                        var a = group.Where(x => x.DialoguePhrases.Count() > 0).FirstOrDefault();
                        if (a == null)
                            a = group.Where(x => x.PhraseCompanys.Count() > 0).FirstOrDefault();
                        if (a == null)
                        {
                            _context.RemoveRange(group);
                            _context.SaveChanges();
                            continue;
                        }
                        var phs = group.Where(x => x.PhraseId != a.PhraseId).ToList();
                        foreach (var ph in phs)
                        {
                            var phraseComp = ph.PhraseCompanys.ToList();
                            var dialogPhrases = ph.DialoguePhrases.ToList();
                            foreach (var phCom in phraseComp)
                            {
                                phCom.PhraseId = a.PhraseId;
                            }

                            foreach (var phD in dialogPhrases)
                            {
                                phD.PhraseId = a.PhraseId;
                            }
                        }
                        _context.RemoveRange(phs);
                        _context.SaveChanges();
                    }
                }
            }

            _context.SaveChanges();

            return Ok();
        }

        [HttpGet("WorkingTimeFill")]
        public async Task<IActionResult> WorkingTimeFill()
        {
            var companyIds = _context.Companys.Select(x => x.CompanyId).ToList();
            foreach (var companyId in companyIds)
            {

                await _compService.AddOneWorkingTimeAsync(companyId, new DateTime(1, 1, 1, 10, 0, 0), new DateTime(1, 1, 1, 19, 0, 0), 1);
                await _compService.AddOneWorkingTimeAsync(companyId, new DateTime(1, 1, 1, 10, 0, 0), new DateTime(1, 1, 1, 19, 0, 0), 2);
                await _compService.AddOneWorkingTimeAsync(companyId, new DateTime(1, 1, 1, 10, 0, 0), new DateTime(1, 1, 1, 19, 0, 0), 3);
                await _compService.AddOneWorkingTimeAsync(companyId, new DateTime(1, 1, 1, 10, 0, 0), new DateTime(1, 1, 1, 19, 0, 0), 4);
                await _compService.AddOneWorkingTimeAsync(companyId, new DateTime(1, 1, 1, 10, 0, 0), new DateTime(1, 1, 1, 19, 0, 0), 5);
                await _compService.AddOneWorkingTimeAsync(companyId, null, null, 6);
                await _compService.AddOneWorkingTimeAsync(companyId, null, null, 0);
                //try
                //{
                //    await _compService.AddOneWorkingTimeAsync(companyId, null, null, 0);
                //    _context.SaveChanges();
                //}
                //catch { }
            }

            //var d = _context.WorkingTimes.Where(x => x.Day == 7).ToList();
            //_context.RemoveRange(d);
            _context.SaveChanges();

            return Ok();
        }


        [HttpGet("PersDet")]
        public async Task PersDet(Guid devId)
        {
            try
            {
                var begTime = DateTime.Now.AddYears(-1);
                var companyIds = _context.Devices.Where(x => x.DeviceId == devId).Select(x => x.CompanyId).Distinct().ToList();

                //---dialogues for devices in company
                var dialogues = _context.Dialogues
                    .Where(p => (companyIds.Contains(p.Device.CompanyId)) && !String.IsNullOrEmpty(p.PersonFaceDescriptor) && p.BegTime >= begTime)
                    .OrderBy(p => p.BegTime)
                    .ToList();

                foreach (var curDialogue in dialogues.Where(p => p.ClientId == null).ToList())
                {
                    var dialoguesProceeded = dialogues
                        .Where(p => p.ClientId != null && p.DeviceId == curDialogue.DeviceId)
                        .ToList();
                    var clientId = FindId(curDialogue, dialoguesProceeded);
                    try
                    {
                        CreateNewClient(curDialogue, clientId);
                    }
                    catch (Exception ex)
                    {
                        var m = ex.Message;
                    }
                }
            }
            catch (Exception e)
            {
                var m = e.Message;
            }
        }

        private Guid? FindId(HBData.Models.Dialogue curDialogue, List<HBData.Models.Dialogue> dialogues, double threshold = 0.42)
        {
            if (!dialogues.Any()) return Guid.NewGuid();
            foreach (var dialogue in dialogues)
            {
                var cosResult = Cos(curDialogue.PersonFaceDescriptor, dialogue.PersonFaceDescriptor);
                System.Console.WriteLine($"Cos distance is -- {cosResult}");
                if (cosResult > threshold)
                    return dialogue.ClientId;
            }
            return Guid.NewGuid();
        }

        [HttpGet("FillDialogueIdInSlideShowSession")]
        public void FillDialogueIdInSlideShowSession(string beg, string end)
        {
             var begTime = _requestFilters.GetBegDate(beg);
             var endTime = _requestFilters.GetEndDate(end);
            var dialogues = _context.Dialogues.Where(x => x.BegTime >= begTime && x.EndTime <= endTime && x.StatusId == 3).ToList();
            foreach (var dialogue in dialogues)
            {
                var slideShowSessions = _context.SlideShowSessions
                    .Where(x => x.BegTime >= dialogue.BegTime && x.BegTime <= dialogue.EndTime && x.DeviceId == dialogue.DeviceId).ToList();
                slideShowSessions.Select(
                    x => 
                    {
                        x.DialogueId = dialogue.DialogueId;
                        return x;
                    }).ToList();
                _context.SaveChanges();
            }
        }



        private Guid? CreateNewClient(HBData.Models.Dialogue curDialogue, Guid? clientId)
        {
            HBData.Models.Company company = _context.Devices
                      .Where(x => x.DeviceId == curDialogue.DeviceId).Select(x => x.Company).FirstOrDefault();
            var findClient = _context.Clients
                        .Where(x => x.ClientId == clientId).FirstOrDefault();
            if (findClient != null)
            {
                findClient.LastDate = DateTime.UtcNow;
                curDialogue.ClientId = findClient.ClientId;
                _context.SaveChanges();
                return findClient.ClientId;
            }

            var dialogueClientProfile = _context.DialogueClientProfiles
                            .FirstOrDefault(x => x.DialogueId == curDialogue.DialogueId);
            if (dialogueClientProfile == null) return null;
            if (dialogueClientProfile.Age == null || dialogueClientProfile.Gender == null) return null;

            var activeStatusId = _context.Statuss
                            .Where(x => x.StatusName == "Active")
                            .Select(x => x.StatusId)
                            .FirstOrDefault();

            double[] faceDescr = new double[0];
            try
            {
                faceDescr = JsonConvert.DeserializeObject<double[]>(curDialogue.PersonFaceDescriptor);
            }
            catch { }
            HBData.Models.Client client = new HBData.Models.Client
            {
                ClientId = (Guid)clientId,
                CompanyId = (Guid)company?.CompanyId,
                CorporationId = company?.CorporationId,
                FaceDescriptor = faceDescr,
                Age = (int)dialogueClientProfile?.Age,
                Avatar = dialogueClientProfile?.Avatar,
                Gender = dialogueClientProfile?.Gender,
                StatusId = activeStatusId
            };
            curDialogue.ClientId = client.ClientId;
            _context.Clients.Add(client);
            _context.SaveChanges();
            return client.ClientId;
        }

        private double VectorNorm(List<double> vector)
        {
            return Math.Sqrt(vector.Sum(p => Math.Pow(p, 2)));
        }

        private double? VectorMult(List<double> vector1, List<double> vector2)
        {
            if (vector1.Count() != vector2.Count()) return null;
            var result = 0.0;
            for (int i = 0; i < vector1.Count(); i++)
            {
                result += vector1[i] * vector2[i];
            }
            return result;
        }

        private double? Cos(List<double> vector1, List<double> vector2)
        {
            return VectorMult(vector1, vector2) / VectorNorm(vector1) / VectorNorm(vector2);
        }

        private double? Cos(string vector1, string vector2)
        {
            var v1 = JsonConvert.DeserializeObject<List<double>>(vector1);
            var v2 = JsonConvert.DeserializeObject<List<double>>(vector2);
            return Cos(v1, v2);
        }

        [HttpGet("CopyDataFromDB")]
        public async Task<IActionResult> CopyDataFromDB()
        {
            var date = DateTime.Now.AddDays(-3);
            var connectionString = "User ID=test_user;Password=test_password;Host=40.69.85.202;Port=5432;Database=test_db;Pooling=true;Timeout=120;CommandTimeout=0;";
            DbContextOptionsBuilder<RecordsContext> dbContextOptionsBuilder = new DbContextOptionsBuilder<RecordsContext>();
            dbContextOptionsBuilder.UseNpgsql(connectionString,
                   dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(UserOperations)));
            var oldContext = new RecordsContext(dbContextOptionsBuilder.Options);

            Dictionary<string, int> result = new Dictionary<string, int>();


            //-1--COMPANIES---
            var oldCompId = oldContext.Companys.Select(x => x.CompanyId).ToList();
            var newCompId = _context.Companys.Select(x => x.CompanyId).ToList();
            var compIdsToAdd = oldCompId.Except(newCompId).ToList();
            List<Company> addComp = oldContext.Companys.Where(x => compIdsToAdd.Contains(x.CompanyId)).ToList();
            var devType = _context.DeviceTypes.FirstOrDefault().DeviceTypeId;

            try
            {
                _context.AddRange(addComp);
                _context.SaveChanges();
                result["companys"] = addComp.Count();

                var devicesToAdd = compIdsToAdd.Select(x => new Device
                {
                    DeviceId = Guid.NewGuid(),
                    CompanyId = x,
                    Code = "AAAAAA",
                    DeviceTypeId = devType,
                    Name = "TEMP DEVICE",
                    StatusId = 3
                });

                _context.AddRange(devicesToAdd);
                _context.SaveChanges();
                result["devices"] = devicesToAdd.Count();

                var work = compIdsToAdd.Select(x => new List<WorkingTime> {
                    new WorkingTime { CompanyId = x, Day = 0 },
                    new WorkingTime { CompanyId = x, Day = 1 },
                    new WorkingTime { CompanyId = x, Day = 2 },
                    new WorkingTime { CompanyId = x, Day = 3 },
                    new WorkingTime { CompanyId = x, Day = 4 },
                    new WorkingTime { CompanyId = x, Day = 5 },
                    new WorkingTime { CompanyId = x, Day = 6 }}
                    );

                _context.AddRange(work);
                _context.SaveChanges();
                result["devices"] = work.Count();
            }
            catch { }

            var devices = _context.Devices.Include(x => x.Company.ApplicationUser)
            .Select(x => new { x.DeviceId, applicationUserIds = x.Company.ApplicationUser.Select(p => p.Id).ToList() }).ToList();

            return Ok(result);
        }

    }
}




