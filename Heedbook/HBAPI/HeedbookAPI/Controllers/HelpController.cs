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
        private readonly CompanyService _compService;
        private readonly RecordsContext _context;
    //    private readonly SftpClient _sftpClient;
    //    private readonly MailSender _mailSender;
    //    private readonly RequestFilters _requestFilters;
    //    private readonly SftpSettings _sftpSettings;
    //    private readonly DBOperations _dbOperation;
    //    private readonly IGenericRepository _repository;


        public HelpController(
            CompanyService compService,
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
            _compService = compService;
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
            var date = DateTime.Now.AddDays(-3);
            var connectionString = "User ID=test_user;Password=test_password;Host=40.69.85.202;Port=5432;Database=test_db;Pooling=true;Timeout=120;CommandTimeout=0;";

            //var connectionString = "User ID=heedbook_user;Password=Oleg&AnnaRulyat_1975;Host=40.69.85.202;Port=5432;Database=heedbook_db;Pooling=true;Timeout=120;CommandTimeout=0;";
            DbContextOptionsBuilder<OldRecordsContext> dbContextOptionsBuilder = new DbContextOptionsBuilder<OldRecordsContext>();
            dbContextOptionsBuilder.UseNpgsql(connectionString,
                   dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(UserOperations)));
            var oldContext = new OldRecordsContext(dbContextOptionsBuilder.Options);

            Dictionary<string, int> result = new Dictionary<string, int>();


            //-1--COMPANIES---
            var oldCompId = oldContext.Companys.Select(x => x.CompanyId).ToList();
            var newCompId = _context.Companys.Select(x => x.CompanyId).ToList();
            var compIdsToAdd = oldCompId.Except(newCompId).ToList();
            List<Old.Models.Company> addComp = oldContext.Companys.Where(x => compIdsToAdd.Contains(x.CompanyId)).ToList();
            var str = JsonConvert.SerializeObject(addComp);
            List<HBData.Models.Company> newComp = JsonConvert.DeserializeObject<List<HBData.Models.Company>>(str);
            var devType = _context.DeviceTypes.FirstOrDefault().DeviceTypeId;

            try
            {
                _context.AddRange(newComp);
                _context.SaveChanges();
                result["companys"] = newComp.Count();

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
                result["devices"] = newComp.Count();

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
                result["devices"] = newComp.Count();
            }
            catch { }

            var devices = _context.Devices.Include(x => x.Company.ApplicationUser)
            .Select(x => new { x.DeviceId, applicationUserIds = x.Company.ApplicationUser.Select(p => p.Id).ToList() }).ToList();


            //-2--USERS---
            var oldUsersId = oldContext.ApplicationUsers.Select(x => x.Id).ToList();
            var newUsersId = _context.ApplicationUsers.Select(x => x.Id).ToList();
            var usersIdsToAdd = oldUsersId.Except(newUsersId).ToList();
            List<Old.Models.ApplicationUser> addUsers = oldContext.ApplicationUsers.Where(x => usersIdsToAdd.Contains(x.Id)).ToList();
            str = JsonConvert.SerializeObject(addUsers);
            List<HBData.Models.ApplicationUser> newUsers = JsonConvert.DeserializeObject<List<HBData.Models.ApplicationUser>>(str);
            try
            {
                _context.AddRange(newUsers);
                _context.SaveChanges();
                result["users"] = newUsers.Count();
            }
            catch { }

            //-2--ROLES---
            var oldUR = oldContext.ApplicationUserRoles.Select(x => x.UserId).ToList();
            var newUR = _context.ApplicationUserRoles.Select(x => x.UserId).ToList();
            var IdsToAdd = oldUR.Except(newUR).ToList();
            List<Old.Models.ApplicationUserRole> add1 = oldContext.ApplicationUserRoles.Where(x => IdsToAdd.Contains(x.UserId)).ToList();
            str = JsonConvert.SerializeObject(add1);
            List<HBData.Models.ApplicationUserRole> newadd = JsonConvert.DeserializeObject<List<HBData.Models.ApplicationUserRole>>(str);
            try
            {
                _context.AddRange(newadd);
                _context.SaveChanges();
                result["users"] = newUsers.Count();
            }
            catch { }

            //-3--ALERTS---
            var oldAlerts = oldContext.Alerts.Where(x => x.CreationDate >= date).Select(x => x.AlertId).ToList();
            var newAlertsId = _context.Alerts.Where(x => x.CreationDate >= date).Select(x => x.AlertId).ToList();
            var alertsIdsToAdd = oldAlerts.Except(newAlertsId).ToList();
            List<Old.Models.Alert> addAlerts = oldContext.Alerts.Where(x => alertsIdsToAdd.Contains(x.AlertId)).ToList();
            List<HBData.Models.Alert> newAlerts = addAlerts.Select(x => new HBData.Models.Alert
            {
                AlertId = x.AlertId,
                AlertTypeId = x.AlertTypeId,
                ApplicationUserId = x.ApplicationUserId,
                CreationDate = x.CreationDate,
                DeviceId = Guid.Empty
            }).ToList();
            try
            {
                _context.AddRange(newAlerts);
                _context.SaveChanges();
                result["alert"] = newAlerts.Count();
            }
            catch { }


            //-4--CAMPAIGN---
            var old1 = oldContext.Campaigns.Select(x => x.CampaignId).ToList();
            var new1 = _context.Campaigns.Select(x => x.CampaignId).ToList();
            var toAddIds1 = old1.Except(new1).ToList();
            List<Old.Models.Campaign> toAdd1 = oldContext.Campaigns.Where(x => toAddIds1.Contains(x.CampaignId)).ToList();
            str = JsonConvert.SerializeObject(toAdd1);
            List<HBData.Models.Campaign> toAdd1_ = JsonConvert.DeserializeObject<List<HBData.Models.Campaign>>(str);
            try
            {
                _context.AddRange(toAdd1_);
                _context.SaveChanges();
                result["campaign"] = toAdd1_.Count();
            }
            catch { }

            //-5--CONTENT---
            var old2 = oldContext.Contents.Select(x => x.ContentId).ToList();
            var new2 = _context.Contents.Select(x => x.ContentId).ToList();
            var toAddIds2 = old2.Except(new2).ToList();
            List<Old.Models.Content> toAdd2 = oldContext.Contents.Where(x => toAddIds2.Contains(x.ContentId)).ToList();
            str = JsonConvert.SerializeObject(toAdd2);
            List<HBData.Models.Content> toAdd2_ = JsonConvert.DeserializeObject<List<HBData.Models.Content>>(str);
            try
            {
                _context.AddRange(toAdd2_);
                _context.SaveChanges();
                result["content"] = toAdd2_.Count();
            }
            catch { }


            //-6--CAMPAIGN CONTENT---
            var old3 = oldContext.CampaignContents.Select(x => x.CampaignContentId).ToList();
            var new3 = _context.CampaignContents.Select(x => x.CampaignContentId).ToList();
            var toAddIds3 = old3.Except(new3).ToList();
            List<Old.Models.CampaignContent> toAdd3 = oldContext.CampaignContents.Where(x => toAddIds3.Contains(x.CampaignContentId)).ToList();
            str = JsonConvert.SerializeObject(toAdd3);
            List<HBData.Models.CampaignContent> toAdd3_ = JsonConvert.DeserializeObject<List<HBData.Models.CampaignContent>>(str);
            try
            {
                _context.AddRange(toAdd3_);
                _context.SaveChanges();
                result["campaign content"] = toAdd3_.Count();
            }
            catch (Exception ex){ var mes = ex.Message; }

            //-7--CAMPAIGN CONTENT answers---
            var old4 = oldContext.CampaignContentAnswers.Where(x => x.Time >= date).Select(x => x.CampaignContentAnswerId).ToList();
            var new4 = _context.CampaignContentAnswers.Where(x => x.Time >= date).Select(x => x.CampaignContentAnswerId).ToList();
            var toAddIds4 = old4.Except(new4).ToList();
            List<Old.Models.CampaignContentAnswer> toAddOld4 = oldContext.CampaignContentAnswers.Where(x => toAddIds4.Contains(x.CampaignContentAnswerId)).ToList();

            List<HBData.Models.CampaignContentAnswer> toAdd4 = toAddOld4.Select(x => new HBData.Models.CampaignContentAnswer
            {
                Answer = x.Answer,
                ApplicationUserId = x.ApplicationUserId,
                CampaignContentAnswerId = x.CampaignContentAnswerId,
                CampaignContentId = x.CampaignContentId,
                Time = x.Time,
                DeviceId = devices.Where(p => p.applicationUserIds.Contains(x.ApplicationUserId)).FirstOrDefault().DeviceId
            }).ToList();
            try
            {
                _context.AddRange(toAdd4);
                _context.SaveChanges();
                result["campaign content answers"] = toAdd4.Count();
            }
            catch (Exception ex) { var mes = ex.Message; }

            //-8--Client---
            var old5 = oldContext.Clients.Select(x => x.ClientId).ToList();
            var new5 = _context.Clients.Select(x => x.ClientId).ToList();
            var toAddIds5 = old5.Except(new5).ToList();
            List<Old.Models.Client> toAdd5 = oldContext.Clients.Where(x => toAddIds5.Contains(x.ClientId)).ToList();
            str = JsonConvert.SerializeObject(toAdd5);
            List<HBData.Models.Client> toAdd5_ = JsonConvert.DeserializeObject<List<HBData.Models.Client>>(str);
            try
            {
                _context.AddRange(toAdd5_);
                _context.SaveChanges();
                result["Client"] = toAdd5_.Count();
            }
            catch (Exception ex) { var mes = ex.Message; }

            //-9--Client Note---
            var old6 = oldContext.ClientNotes.Where(x => x.CreationDate >= date).Select(x => x.ClientNoteId).ToList();
            var new6 = _context.ClientNotes.Where(x => x.CreationDate >= date).Select(x => x.ClientNoteId).ToList();
            var toAddIds6 = old6.Except(new6).ToList();
            List<Old.Models.ClientNote> toAdd6 = oldContext.ClientNotes.Where(x => toAddIds6.Contains(x.ClientNoteId)).ToList();
            str = JsonConvert.SerializeObject(toAdd6);
            List<HBData.Models.ClientNote> toAdd6_ = JsonConvert.DeserializeObject<List<HBData.Models.ClientNote>>(str);
            try
            {
                _context.AddRange(toAdd6_);
                _context.SaveChanges();
                result["Client note"] = toAdd6_.Count();
            }
            catch (Exception ex) { var mes = ex.Message; }

            //-10--Dialogues---
            var old7 = oldContext.Dialogues.Select(x => x.DialogueId).ToList();
            var new7 = _context.Dialogues.Select(x => x.DialogueId).ToList();
            var toAddIdsDevices = old7.Except(new7).ToList();
            List<Old.Models.Dialogue> toAddOld7 = oldContext.Dialogues.Where(x => toAddIdsDevices.Contains(x.DialogueId)).ToList();

            List<HBData.Models.Dialogue> toAdd7 = toAddOld7.Select(x => new HBData.Models.Dialogue
            {
                DialogueId = x.DialogueId,
                ApplicationUserId = x.ApplicationUserId,
                BegTime = x.BegTime,
                ClientId = x.ClientId,
                Comment = x.Comment,
                CreationTime = x.CreationTime,
                EndTime = x.EndTime,
                InStatistic = x.InStatistic,
                StatusId = x.StatusId,
                LanguageId = x.LanguageId,
                PersonFaceDescriptor = x.PersonFaceDescriptor,
                DeviceId = devices.Where(p => p.applicationUserIds.Contains(x.ApplicationUserId)).FirstOrDefault().DeviceId
            }).ToList();
            try
            {
                _context.AddRange(toAdd7);
                _context.SaveChanges();
                result["Dialogue"] = toAdd7.Count();
            }
            catch (Exception ex) { var e = ex.Message; }

            //-11--Phrase---
            var old8 = oldContext.Phrases.Select(x => x.PhraseId).ToList();
            var new8 = _context.Phrases.Select(x => x.PhraseId).ToList();
            var toAddIds8 = old8.Except(new8).ToList();
            List<Old.Models.Phrase> toAddOld8 = oldContext.Phrases.Where(x => toAddIds8.Contains(x.PhraseId)).ToList();
            str = JsonConvert.SerializeObject(toAddOld8);
            List<HBData.Models.Phrase> toAdd8_ = JsonConvert.DeserializeObject<List<HBData.Models.Phrase>>(str);
            try
            {
                _context.AddRange(toAdd8_);
                _context.SaveChanges();
                result["Phrase"] = toAdd8_.Count();
            }
            catch (Exception ex) { var e = ex.Message; }

            //-12--PhraseCompany---
            var old9 = oldContext.PhraseCompanys.Select(x => x.PhraseCompanyId).ToList();
            var new9 = _context.PhraseCompanys.Select(x => x.PhraseCompanyId).ToList();
            var toAddIds9 = old9.Except(new9).ToList();
            List<Old.Models.PhraseCompany> toAddOld9 = oldContext.PhraseCompanys.Where(x => toAddIds9.Contains(x.PhraseCompanyId)).ToList();
            str = JsonConvert.SerializeObject(toAddOld9);
            List<HBData.Models.PhraseCompany> toAdd9_ = JsonConvert.DeserializeObject<List<HBData.Models.PhraseCompany>>(str);
            try
            {
                _context.AddRange(toAdd9_);
                _context.SaveChanges();
                result["PhraseCompany"] = toAdd9_.Count();
            }
            catch (Exception ex) { var e = ex.Message; }


            //-13--Session---
            var old10 = oldContext.Sessions.Where(x => x.BegTime >= date).Select(x => x.SessionId).ToList();
            var new10 = _context.Sessions.Where(x => x.BegTime >= date).Select(x => x.SessionId).ToList();
            var toAddIds10 = old10.Except(new10).ToList();
            List<Old.Models.Session> toAddOld10 = oldContext.Sessions.Where(x => toAddIds10.Contains(x.SessionId)).ToList();

            List<HBData.Models.Session> toAdd10 = toAddOld10.Select(x => new HBData.Models.Session
            {
                SessionId = x.SessionId,
                ApplicationUserId = x.ApplicationUserId,
                BegTime = x.BegTime,
                EndTime = x.EndTime,
                StatusId = x.StatusId,
                IsDesktop = x.IsDesktop,
                DeviceId = devices.Where(p => p.applicationUserIds.Contains(x.ApplicationUserId)).FirstOrDefault().DeviceId
            }).ToList();
            try
            {
                _context.AddRange(toAdd10);
                _context.SaveChanges();
                result["Session"] = toAdd10.Count();
            }
            catch (Exception ex) { var e = ex.Message; }


            //-14--SLIDE SHOW SESSION---
            var old11 = oldContext.SlideShowSessions.Where(x => x.BegTime >= date).Select(x => x.SlideShowSessionId).ToList();
            var new11 = _context.SlideShowSessions.Where(x => x.BegTime >= date).Select(x => x.SlideShowSessionId).ToList();
            var toAddIds11 = old11.Except(new11).ToList();
            List<Old.Models.SlideShowSession> toAddOld11 = oldContext.SlideShowSessions.Where(x => toAddIds11.Contains(x.SlideShowSessionId)).ToList();

            List<HBData.Models.SlideShowSession> toAdd11 = toAddOld11.Select(x => new HBData.Models.SlideShowSession
            {
                BegTime = x.BegTime,
                ContentType = x.ContentType,
                EndTime = x.EndTime,
                IsPoll = x.IsPoll,
                SlideShowSessionId = x.SlideShowSessionId,
                Url = x.Url,
                ApplicationUserId = x.ApplicationUserId,
                CampaignContentId = x.CampaignContentId,
                DeviceId = devices.Where(p => p.applicationUserIds.Contains((Guid)x.ApplicationUserId)).FirstOrDefault().DeviceId
            }).ToList();
            try
            {
                _context.AddRange(toAdd11);
                _context.SaveChanges();
                result["SlideShowSession"] = toAdd11.Count();
            }
            catch { }

            //-15--DialogueAudio---
            var toAddIds15 = oldContext.DialogueAudios.Where(x => toAddIdsDevices.Contains((Guid)x.DialogueId)).Select(x => x.DialogueAudioId).ToList();
            List<Old.Models.DialogueAudio> toAddOld15 = oldContext.DialogueAudios.Where(x => toAddIds15.Contains(x.DialogueAudioId)).ToList();
            str = JsonConvert.SerializeObject(toAddOld15);
            List<HBData.Models.DialogueAudio> toAdd15_ = JsonConvert.DeserializeObject<List<HBData.Models.DialogueAudio>>(str);
            try
            {
                _context.AddRange(toAdd15_);
                _context.SaveChanges();
                result["DialogueAudio"] = toAdd15_.Count();
            }
            catch (Exception ex) { var e = ex.Message; }

            //-16--DialogueClientProfiles---
            var toAddIds16 = oldContext.DialogueClientProfiles.Where(x => toAddIdsDevices.Contains((Guid)x.DialogueId)).Select(x => x.DialogueClientProfileId).ToList();
            List<Old.Models.DialogueClientProfile> toAddOld16 = oldContext.DialogueClientProfiles.Where(x => toAddIds16.Contains(x.DialogueClientProfileId)).ToList();
            str = JsonConvert.SerializeObject(toAddOld16);
            List<HBData.Models.DialogueClientProfile> toAdd16_ = JsonConvert.DeserializeObject<List<HBData.Models.DialogueClientProfile>>(str);
            try
            {
                _context.AddRange(toAdd16_);
                _context.SaveChanges();
                result["DialogueClientProfiles"] = toAdd16_.Count();
            }
            catch (Exception ex) { var e = ex.Message; }

            //-17--DialogueClientSatisfactions---
            var toAddIds17 = oldContext.DialogueClientSatisfactions.Where(x => toAddIdsDevices.Contains((Guid)x.DialogueId)).Select(x => x.DialogueClientSatisfactionId).ToList();
            List<Old.Models.DialogueClientSatisfaction> toAddOld17
                = oldContext.DialogueClientSatisfactions.Where(x => toAddIds17.Contains(x.DialogueClientSatisfactionId)).ToList();
            str = JsonConvert.SerializeObject(toAddOld17);
            List<HBData.Models.DialogueClientSatisfaction> toAdd17_ = JsonConvert.DeserializeObject<List<HBData.Models.DialogueClientSatisfaction>>(str);
            try
            {
                _context.AddRange(toAdd17_);
                _context.SaveChanges();
                result["DialogueClientSatisfactions"] = toAdd17_.Count();
            }
            catch (Exception ex) { var e = ex.Message; }

            //-18--DialogueFrames---
            var toAddIds18 = oldContext.DialogueFrames.Where(x => toAddIdsDevices.Contains((Guid)x.DialogueId)).Select(x => x.DialogueFrameId).ToList();
            List<Old.Models.DialogueFrame> toAddOld18
                = oldContext.DialogueFrames.Where(x => toAddIds18.Contains(x.DialogueFrameId)).ToList();
            str = JsonConvert.SerializeObject(toAddOld18);
            List<HBData.Models.DialogueFrame> toAdd18_ = JsonConvert.DeserializeObject<List<HBData.Models.DialogueFrame>>(str);
            try
            {
                _context.AddRange(toAdd18_);
                _context.SaveChanges();
                result["DialogueFrames"] = toAdd18_.Count();
            }
            catch (Exception ex) { var e = ex.Message; }

            //-19--DialogueHint---
            var toAddIds19 = oldContext.DialogueHints.Where(x => toAddIdsDevices.Contains((Guid)x.DialogueId)).Select(x => x.DialogueHintId).ToList();
            List<Old.Models.DialogueHint> toAddOld19
                = oldContext.DialogueHints.Where(x => toAddIds19.Contains(x.DialogueHintId)).ToList();
            str = JsonConvert.SerializeObject(toAddOld19);
            List<HBData.Models.DialogueHint> toAdd19_ = JsonConvert.DeserializeObject<List<HBData.Models.DialogueHint>>(str);
            try
            {
                _context.AddRange(toAdd19_);
                _context.SaveChanges();
                result["DialogueHint"] = toAdd19_.Count();
            }
            catch (Exception ex) { var e = ex.Message; }

            //-20--DialogueInterval---
            var toAddIds20 = oldContext.DialogueIntervals.Where(x => toAddIdsDevices.Contains((Guid)x.DialogueId)).Select(x => x.DialogueIntervalId).ToList();
            List<Old.Models.DialogueInterval> toAddOld20
                = oldContext.DialogueIntervals.Where(x => toAddIds20.Contains(x.DialogueIntervalId)).ToList();
            str = JsonConvert.SerializeObject(toAddOld20);
            List<HBData.Models.DialogueInterval> toAdd20_ = JsonConvert.DeserializeObject<List<HBData.Models.DialogueInterval>>(str);
            try
            {
                _context.AddRange(toAdd20_);
                _context.SaveChanges();
                result["DialogueInterval"] = toAdd20_.Count();
            }
            catch (Exception ex) { var e = ex.Message; }

            //-21--DialoguePhraseCounts---
            var toAddIds21 = oldContext.DialoguePhraseCounts.Where(x => toAddIdsDevices.Contains((Guid)x.DialogueId)).Select(x => x.DialoguePhraseCountId).ToList();
            List<Old.Models.DialoguePhraseCount> toAddOld21
                = oldContext.DialoguePhraseCounts.Where(x => toAddIds21.Contains(x.DialoguePhraseCountId)).ToList();
            str = JsonConvert.SerializeObject(toAddOld21);
            List<HBData.Models.DialoguePhraseCount> toAdd21_ = JsonConvert.DeserializeObject<List<HBData.Models.DialoguePhraseCount>>(str);
            try
            {
                _context.AddRange(toAdd21_);
                _context.SaveChanges();
                result["DialoguePhraseCounts"] = toAdd21_.Count();
            }
            catch (Exception ex) { var e = ex.Message; }

            //-22--DialoguePhrases---
            var toAddIds22 = oldContext.DialoguePhrases.Where(x => toAddIdsDevices.Contains((Guid)x.DialogueId)).Select(x => x.DialoguePhraseId).ToList();
            List<Old.Models.DialoguePhrase> toAddOld22
                = oldContext.DialoguePhrases.Where(x => toAddIds22.Contains(x.DialoguePhraseId)).ToList();
            str = JsonConvert.SerializeObject(toAddOld22);
            List<HBData.Models.DialoguePhrase> toAdd22_ = JsonConvert.DeserializeObject<List<HBData.Models.DialoguePhrase>>(str);
            try
            {
                _context.AddRange(toAdd22_);
                _context.SaveChanges();
                result["DialoguePhrases"] = toAdd22_.Count();
            }
            catch (Exception ex) { var e = ex.Message; }

            //-23--DialogueSpeech---
            var toAddIds23 = oldContext.DialogueSpeechs.Where(x => toAddIdsDevices.Contains((Guid)x.DialogueId)).Select(x => x.DialogueSpeechId).ToList();
            List<Old.Models.DialogueSpeech> toAddOld23
                = oldContext.DialogueSpeechs.Where(x => toAddIds23.Contains(x.DialogueSpeechId)).ToList();
            str = JsonConvert.SerializeObject(toAddOld23);
            List<HBData.Models.DialogueSpeech> toAdd23_ = JsonConvert.DeserializeObject<List<HBData.Models.DialogueSpeech>>(str);
            try
            {
                _context.AddRange(toAdd23_);
                _context.SaveChanges();
                result["DialogueSpeech"] = toAdd23_.Count();
            }
            catch (Exception ex) { var e = ex.Message; }

            //-24--DialogueVisuals---
            var toAddIds24 = oldContext.DialogueVisuals.Where(x => toAddIdsDevices.Contains((Guid)x.DialogueId)).Select(x => x.DialogueVisualId).ToList();
            List<Old.Models.DialogueVisual> toAddOld24
                = oldContext.DialogueVisuals.Where(x => toAddIds24.Contains(x.DialogueVisualId)).ToList();
            str = JsonConvert.SerializeObject(toAddOld24);
            List<HBData.Models.DialogueVisual> toAdd24_ = JsonConvert.DeserializeObject<List<HBData.Models.DialogueVisual>>(str);
            try
            {
                _context.AddRange(toAdd24_);
                _context.SaveChanges();
                result["DialogueVisuals"] = toAdd24_.Count();
            }
            catch (Exception ex) { var e = ex.Message; }

            //-25--DialogueWord---
            var toAddIds25 = oldContext.DialogueWords.Where(x => toAddIdsDevices.Contains((Guid)x.DialogueId)).Select(x => x.DialogueWordId).ToList();
            List<Old.Models.DialogueWord> toAddOld25
                = oldContext.DialogueWords.Where(x => toAddIds25.Contains(x.DialogueWordId)).ToList();
            str = JsonConvert.SerializeObject(toAddOld25);
            List<HBData.Models.DialogueWord> toAdd25_ = JsonConvert.DeserializeObject<List<HBData.Models.DialogueWord>>(str);
            try
            {
                _context.AddRange(toAdd25_);
                _context.SaveChanges();
                result["DialogueWord"] = toAdd25_.Count();
            }
            catch (Exception ex) { var e = ex.Message; }

            return Ok(result);
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


        [HttpGet("PhraseClear")]
        public async Task<IActionResult> PhraseClear()
        {
            var phrases = _context.Phrases.Include(x => x.PhraseCompanys).Include(x => x.DialoguePhrases).GroupBy(x => x.PhraseText).ToList();

            foreach (var group in phrases)
            {
                if(group.Count()>1)
                {
                    var templ = group.Where(x => x.IsTemplate == true).FirstOrDefault();
                    if(templ != null)
                    {
                        var phs = group.Where(x => x.IsTemplate == false).ToList();
                        foreach(var ph in phs)
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
                        if(a == null)
                            a = group.Where(x => x.PhraseCompanys.Count() > 0).FirstOrDefault();
                        if(a == null)
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


        [HttpGet("SalesStageAdd")]
        public IActionResult SalesStageAdd()
        {
            List<Temp> arr = new List<Temp> {
//                 new Temp { phrase = "Здравствуйте	", id = 1},
//new Temp { phrase = "Будем рады видеть вас снова	", id = 7},
//new Temp { phrase = "До свидания ", id = 7},
//new Temp { phrase = "Добро пожаловать    ", id = 1},
//new Temp { phrase = "Как я могу к вам обращаться ", id = 1},
//new Temp { phrase = "Меня зовут  ", id = 1},
//new Temp { phrase = "Могу ли еще чем-то помочь   ", id = 6},
//new Temp { phrase = "Чем могу вам помочь ", id = 1},
//new Temp { phrase = "Буду рад помочь	", id = 1},
//new Temp { phrase = "Ваше обращение ценно    ", id = 2},
//new Temp { phrase = "Выясню это для вас  ", id = 2},
//new Temp { phrase = "Извините за ожидание	", id = 1},
//new Temp { phrase = "Обязательно разберемся  ", id = 2},
//new Temp { phrase = "Обязательно решу вопрос	", id = 2},
//new Temp { phrase = "Позвольте предложить    ", id = 3},
//new Temp { phrase = "Присаживайтесь	", id = 1},
//new Temp { phrase = "Располагайтесь	", id = 1},
//new Temp { phrase = "Сделаю все что смогу    ", id = 1},
//new Temp { phrase = "Угощайтесь	", id = 1},
//new Temp { phrase = "Акция	", id = 3},
//new Temp { phrase = "Бонусы	", id = 3},
//new Temp { phrase = "Доставка	", id = 3},
//new Temp { phrase = "Инвестиции	", id = 3},
//new Temp { phrase = "Интернет-банк	", id = 3},
//new Temp { phrase = "Ипотека	", id = 3},
//new Temp { phrase = "Контракт	", id = 3},
//new Temp { phrase = "Кредит	", id = 3},
//new Temp { phrase = "Кредитная карта ", id = 3},
//new Temp { phrase = "Личный менеджер ", id = 5},
//new Temp { phrase = "Мобильный банк  ", id = 5},
//new Temp { phrase = "Овердрафт	", id = 5},
//new Temp { phrase = "Ограниченное предложение    ", id = 4},
//new Temp { phrase = "Пакет услуг ", id = 4},
//new Temp { phrase = "Прибыль	", id = 5},
//new Temp { phrase = "Процент на остаток	", id = 5},
//new Temp { phrase = "Сбережения	", id = 5},
//new Temp { phrase = "СМС оповещение  ", id = 5},
//new Temp { phrase = "Специальное предложение ", id = 4},
//new Temp { phrase = "Страхование	", id = 3},
//new Temp { phrase = "Счет	", id = 3},
//new Temp { phrase = "Тариф	", id = 3},
//new Temp { phrase = "Эксклюзив	", id = 4},
//new Temp { phrase = "актуален ли для вас ", id = 4},
//new Temp { phrase = "улучшение жилищных условий	", id = 3},
//new Temp { phrase = "благодарю за обращение	", id = 2},
//new Temp { phrase = "для обсуждения условий и следующих шагов    ", id = 6},
//new Temp { phrase = "бесплатно подключается  ", id = 4},
//new Temp { phrase = "широкая линейка кредитных продуктов ", id = 3},
//new Temp { phrase = "задать вам несколько вопросов   ", id = 2},
//new Temp { phrase = "планируете приобрести   ", id = 2},
//new Temp { phrase = "планируете потратить средства	", id = 2},
//new Temp { phrase = "какая сумма вас интересует  ", id = 2},
//new Temp { phrase = "семейный бюджет ", id = 3},
//new Temp { phrase = "спасибо, что обратились в наш банк!	", id = 1},
//new Temp { phrase = "всего доброго, до свидания!	", id = 7},
//new Temp { phrase = "улучшить условия кредитования	", id = 4},
//new Temp { phrase = "рад вам помочь	", id = 6},
//new Temp { phrase = "важно укреплять наши отношения  ", id = 4},
//new Temp { phrase = "создавать комфортные условия	", id = 4},
//new Temp { phrase = "интересное сотрудничество   ", id = 4},
//new Temp { phrase = "позвольте я задам	", id = 4},
//new Temp { phrase = "хотите оформить ", id = 2},
//new Temp { phrase = "могу я уточнить	", id = 4},
//new Temp { phrase = "приятного вам дня	", id = 7},
//new Temp { phrase = "доброе утро ", id = 1},
//new Temp { phrase = "добрый день ", id = 1},
//new Temp { phrase = "добрый вечер    ", id = 1},
//new Temp { phrase = "как я могу к вам обращаться ", id = 1},
//new Temp { phrase = "что вас интересует	", id = 2},
//new Temp { phrase = "не выходя из дома   ", id = 3},
//new Temp { phrase = "по льготным тарифам	", id = 3},
//new Temp { phrase = "совершенно бесплатно    ", id = 4},
//new Temp { phrase = "в чем причина вашего отказа	", id = 4},
//new Temp { phrase = "позвольте задать вам несколько вопросов	", id = 2},
//new Temp { phrase = "наиболее выгодные условия	", id = 4},
//new Temp { phrase = "бесплатно предоставляется   ", id = 4},
//new Temp { phrase = "одни из лучших ставок   ", id = 4},
//new Temp { phrase = "гибкие условия по управлению    ", id = 4},
//new Temp { phrase = "позвольте уточнить  ", id = 4},
//new Temp { phrase = "позвольте предложить    ", id = 4},
//new Temp { phrase = "спасибо, что обратились в наш банк	", id = 1},
//new Temp { phrase = "всего доброго, до свидания	", id = 7},
//new Temp { phrase = "как вам будет удобнее   ", id = 6},
//new Temp { phrase = "хочу поблагодарить  ", id = 6},
//new Temp { phrase = "подскажите, пожалуйста	", id = 2},
//new Temp { phrase = "персональный менеджер   ", id = 5},
//new Temp { phrase = "значимый клиент ", id = 4},
//new Temp { phrase = "премиальный сервис  ", id = 5},
//new Temp { phrase = "комфортное и персональное обслуживание  ", id = 5},
//new Temp { phrase = "профессиональное решение    ", id = 3},
//new Temp { phrase = "финансовые цели ", id = 3},
//new Temp { phrase = "персональное предложение    ", id = 5},
//new Temp { phrase = "спасибо, что уделили время	", id = 2},
//new Temp { phrase = "рады предложить вам	", id = 5},
//new Temp { phrase = "с помощью ипотеки	", id = 3},
//new Temp { phrase = "ипотечный кредит    ", id = 3},
//new Temp { phrase = "в любом отделении	", id = 6},
//new Temp { phrase = "согласие на хранение, передачу и обработку персональных данных	", id = 6},
//new Temp { phrase = "оплачивать счета    ", id = 5},
//new Temp { phrase = "просматривать движения денежных средств ", id = 5},
//new Temp { phrase = "контролировать операции ", id = 5},
//new Temp { phrase = "полный состав семьи	", id = 5}

new Temp { phrase = "погашать кредит ", id = 3},
new Temp { phrase = "положительная кредитная история	", id = 2},
new Temp { phrase = "процентная ставка   ", id = 3},
new Temp { phrase = "рассмотрение заявки ", id = 3},
new Temp { phrase = "изменить срок выплат	", id = 4},
new Temp { phrase = "погашение кредитов  ", id = 4},
new Temp { phrase = "по кредитной политике	", id = 4},
new Temp { phrase = "дополнительные денежные средства	", id = 3},
new Temp { phrase = "овердрафт	", id = 3},
new Temp { phrase = "запасной кошелек    ", id = 4},
new Temp { phrase = "возникновение непредвиденных расходов	", id = 4},
new Temp { phrase = "вклады с более высокой процентной ставкой   ", id = 5},
new Temp { phrase = "в курсе движений по счету	", id = 5},
new Temp { phrase = "текущий остаток ", id = 3},
new Temp { phrase = "подключаем	", id = 5},
new Temp { phrase = "премиальный клиент  ", id = 5},
new Temp { phrase = "новая продуктовая линейка	", id = 5},
new Temp { phrase = "сохранить и приумножить денежные средства	", id = 5},
new Temp { phrase = "хотели бы разместить	", id = 2},
new Temp { phrase = "предлагаю вам оформить	", id = 3},
new Temp { phrase = "это займет не более ", id = 2},
new Temp { phrase = "сберегательный счет ", id = 3},
new Temp { phrase = "свободно распоряжаться своими средствами    ", id = 3},
new Temp { phrase = "получать доход на фактический остаток	", id = 3},
new Temp { phrase = "оформляем	", id = 6},
new Temp { phrase = "равная юридическая сила	", id = 4},
new Temp { phrase = "комплексный договор ", id = 6},
new Temp { phrase = "давайте подпишем    ", id = 6},
new Temp { phrase = "бюджетная организация   ", id = 3},
new Temp { phrase = "карта мир   ", id = 3},
new Temp { phrase = "программа лояльности    ", id = 5},
new Temp { phrase = "прямо сейчас    ", id = 6},
new Temp { phrase = "неименная классическая карта	", id = 3},
new Temp { phrase = "премиальное обслуживание    ", id = 5},
new Temp { phrase = "инвестирование	", id = 3},
new Temp { phrase = "высокая доходность  ", id = 3},
new Temp { phrase = "полная сохранность денежных средств ", id = 3},
new Temp { phrase = "портфельный подход  ", id = 3},
new Temp { phrase = "эксперт по ипотеке	", id = 3},
new Temp { phrase = "интернет-банк	", id = 3},
new Temp { phrase = "мобильный банк  ", id = 5},
new Temp { phrase = "sms-оповещение	", id = 3},
new Temp { phrase = "программа комплексной финансовой защиты ", id = 5},
new Temp { phrase = "защита от рисков	", id = 3},
new Temp { phrase = "потеря трудоспособности ", id = 5},
new Temp { phrase = "потеря работы   ", id = 5},
new Temp { phrase = "эксперт по ипотеке	", id = 5},
new Temp { phrase = "интернет-банк	", id = 3},
new Temp { phrase = "мобильный банк  ", id = 3},
new Temp { phrase = "актуален ли для вас ", id = 4},
new Temp { phrase = "улучшение жилищных условий	", id = 5},
new Temp { phrase = "благодарю за обращение	", id = 2},
new Temp { phrase = "для обсуждения условий и следующих шагов    ", id = 6},
new Temp { phrase = "бесплатно подключается  ", id = 5},
new Temp { phrase = "широкая линейка кредитных продуктов ", id = 3},
new Temp { phrase = "задать вам несколько вопросов   ", id = 2},
new Temp { phrase = "планируете приобрести   ", id = 2},
new Temp { phrase = "планируете потратить средства	", id = 2},
new Temp { phrase = "какая сумма вас интересует  ", id = 2},
new Temp { phrase = "семейный бюджет ", id = 2},
new Temp { phrase = "всего доброго, до свидания!	", id = 7},
new Temp { phrase = "улучшить условия кредитования	", id = 3},
new Temp { phrase = "рад вам помочь	", id = 2},
new Temp { phrase = "важно укреплять наши отношения  ", id = 4},
new Temp { phrase = "создавать комфортные условия	", id = 4},
new Temp { phrase = "интересное сотрудничество   ", id = 4},
new Temp { phrase = "позвольте я задам	", id = 2},
new Temp { phrase = "хотите оформить ", id = 2},
new Temp { phrase = "могу я уточнить	", id = 2},
new Temp { phrase = "приятного вам дня	", id = 7},
new Temp { phrase = "доброе утро ", id = 1},
new Temp { phrase = "добрый день ", id = 1},
new Temp { phrase = "добрый вечер    ", id = 1},
new Temp { phrase = "как я могу к вам обращаться ", id = 1},
new Temp { phrase = "что вас интересует	", id = 2},
new Temp { phrase = "не выходя из дома   ", id = 3},
new Temp { phrase = "по льготным тарифам	", id = 3},
new Temp { phrase = "совершенно бесплатно    ", id = 3},
new Temp { phrase = "в чем причина вашего отказа	", id = 4},
new Temp { phrase = "позвольте задать вам несколько вопросов	", id = 2},
new Temp { phrase = "наиболее выгодные условия	", id = 3},
new Temp { phrase = "бесплатно предоставляется   ", id = 3},
new Temp { phrase = "одни из лучших ставок   ", id = 4},
new Temp { phrase = "гибкие условия по управлению    ", id = 4},
new Temp { phrase = "позвольте уточнить  ", id = 4},
new Temp { phrase = "позвольте предложить    ", id = 4},
new Temp { phrase = "спасибо, что обратились в наш банк	", id = 1},
new Temp { phrase = "всего доброго, до свидания	", id = 7},
new Temp { phrase = "как вам будет удобнее   ", id = 6},
new Temp { phrase = "хочу поблагодарить  ", id = 7},
new Temp { phrase = "подскажите, пожалуйста	", id = 2},
new Temp { phrase = "персональный менеджер   ", id = 2},
new Temp { phrase = "значимый клиент ", id = 4},
new Temp { phrase = "премиальный сервис  ", id = 5},
new Temp { phrase = "комфортное и персональное обслуживание  ", id = 5},
new Temp { phrase = "профессиональное решение    ", id = 3},
new Temp { phrase = "финансовые цели ", id = 2},
new Temp { phrase = "персональное предложение    ", id = 3},
new Temp { phrase = "спасибо, что уделили время	", id = 4},
new Temp { phrase = "рады предложить вам	", id = 3},
new Temp { phrase = "с помощью ипотеки	", id = 5},
new Temp { phrase = "ипотечный кредит    ", id = 3},
new Temp { phrase = "согласие на хранение, передачу и обработку персональных данных	", id = 6},
new Temp { phrase = "оплачивать счета    ", id = 5},
new Temp { phrase = "просматривать движения денежных средств ", id = 5},
new Temp { phrase = "контролировать операции ", id = 5},
new Temp { phrase = "полный состав семьи	", id = 5},
new Temp { phrase = "погашать кредит ", id = 4},
new Temp { phrase = "положительная кредитная история	", id = 2},
new Temp { phrase = "процентная ставка   ", id = 3},
new Temp { phrase = "рассмотрение заявки ", id = 3},
new Temp { phrase = "изменить срок выплат	", id = 4},
new Temp { phrase = "погашение кредитов  ", id = 5},
new Temp { phrase = "по кредитной политике	", id = 5},
new Temp { phrase = "дополнительные денежные средства	", id = 5},
new Temp { phrase = "овердрафт	", id = 5},
new Temp { phrase = "запасной кошелек    ", id = 5},
new Temp { phrase = "возникновение непредвиденных расходов	", id = 5},
new Temp { phrase = "вклады с более высокой процентной ставкой   ", id = 5},
new Temp { phrase = "в курсе движений по счету	", id = 5},
new Temp { phrase = "текущий остаток ", id = 2},
new Temp { phrase = "подключаем	", id = 6},
new Temp { phrase = "премиальный клиент  ", id = 2},
new Temp { phrase = "новая продуктовая линейка	", id = 5},
new Temp { phrase = "сохранить и приумножить денежные средства	", id = 5},
new Temp { phrase = "хотели бы разместить	", id = 2},
new Temp { phrase = "предлагаю вам оформить	", id = 4},
new Temp { phrase = "это займет не более ", id = 3},
new Temp { phrase = "сберегательный счет ", id = 3},
new Temp { phrase = "свободно распоряжаться своими средствами    ", id = 3},
new Temp { phrase = "получать доход на фактический остаток	", id = 3},
new Temp { phrase = "оформляем	", id = 6},
new Temp { phrase = "равная юридическая сила	", id = 6},
new Temp { phrase = "комплексный договор ", id = 6},
new Temp { phrase = "давайте подпишем    ", id = 6},
new Temp { phrase = "бюджетная организация   ", id = 6},
new Temp { phrase = "карта мир   ", id = 3},
new Temp { phrase = "программа лояльности    ", id = 5},
new Temp { phrase = "прямо сейчас    ", id = 6},
new Temp { phrase = "неименная классическая карта	", id = 5},
new Temp { phrase = "премиальное обслуживание    ", id = 5},
new Temp { phrase = "инвестирование	", id = 5},
new Temp { phrase = "высокая доходность  ", id = 3},
new Temp { phrase = "полная сохранность денежных средств ", id = 3},
new Temp { phrase = "портфельный подход  ", id = 3},
new Temp { phrase = "эксперт по ипотеке	", id = 5},
new Temp { phrase = "интернет-банк	", id = 3},
new Temp { phrase = "мобильный банк  ", id = 3},
new Temp { phrase = "sms-оповещение	", id = 3},
new Temp { phrase = "программа комплексной финансовой защиты ", id = 5},
new Temp { phrase = "защита от рисков	", id = 5},
new Temp { phrase = "потеря трудоспособности ", id = 5},
new Temp { phrase = "потеря работы   ", id = 5}
            };
            int counter = 0;
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
                        counter++;
                    }
                }
            catch (Exception ex){ var mes = ex.Message; }
            }
            return Ok(counter);
        }

        public class Temp
        {
            public int id;
            public string phrase;
        }
    }
}




