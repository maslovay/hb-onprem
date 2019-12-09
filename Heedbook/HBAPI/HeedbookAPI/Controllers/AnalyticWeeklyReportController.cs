using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using UserOperations.Services;
using UserOperations.Models.AnalyticModels;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using HBData;
using UserOperations.Utils;
using HBLib.Utils;
using HBData.Models;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticWeeklyReportController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
        private readonly DBOperationsWeeklyReport _dbOperation;
        private readonly IRequestFilters _requestFilters;

        public AnalyticWeeklyReportController(
            IConfiguration config,
            ILoginService loginService,
            RecordsContext context,
            DBOperationsWeeklyReport dbOperation,
            IRequestFilters requestFilters
            )
        {
            _config = config;
            _loginService = loginService;
            _context = context;
            _dbOperation = dbOperation;
            _requestFilters = requestFilters;
        }

        [HttpGet("User")]
        public new IActionResult User(
            [FromHeader] string Authorization,
            [FromQuery(Name = "applicationUserId")] Guid userId )
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");

                var begTime = DateTime.Now.AddDays(-7);
                var emplyeeRoleId = _context.Roles.FirstOrDefault(x => x.Name == "Employee").Id;
                var jsonToReturn = new Dictionary<string, object>();
                var companyId = _context.ApplicationUsers.Where(p => p.Id == userId).FirstOrDefault().CompanyId;
                var corporationId = _context.Companys.Where(p => p.CompanyId == companyId).FirstOrDefault()?.CorporationId;
                var userIdsInCorporation = corporationId != null? _context.Companys
                        .Include(p => p.ApplicationUser)
                        .ThenInclude(u => u.UserRoles)
                        .Where(p => p.CorporationId == corporationId)
                        .SelectMany(p => p.ApplicationUser.Where(u => u.UserRoles.Select(r => r.RoleId).Contains(emplyeeRoleId)).Select(u => u.Id)).ToList() : null;
                var userIdsInCompany = _context.ApplicationUsers                
                        .Where(p => p.CompanyId == companyId && p.UserRoles.Select(r => r.RoleId).Contains(emplyeeRoleId)).Select(u => u.Id).ToList();

                //----ALL FOR WEEK Corporation---
                var sessionsCorporation = userIdsInCorporation != null? 
                        _context.VSessionUserWeeklyReports.Where(p => userIdsInCorporation.Contains(p.AspNetUserId) && p.Day > begTime).ToList() : null;
                var sessionsCorporationOld = userIdsInCorporation != null? 
                        _context.VSessionUserWeeklyReports.Where(p => userIdsInCorporation.Contains(p.AspNetUserId) && p.Day <= begTime).ToList() : null;
                var dialoguesCorporation = userIdsInCorporation != null? 
                        _context.VWeeklyUserReports.Where(p => userIdsInCorporation.Contains(p.AspNetUserId) && p.Day > begTime).ToList() : null;
                var dialoguesCorporationOld = userIdsInCorporation != null? 
                        _context.VWeeklyUserReports.Where(p => userIdsInCorporation.Contains(p.AspNetUserId) && p.Day <= begTime).ToList() : null;
                //----ALL FOR WEEK Company---
                var sessionsCompany = _context.VSessionUserWeeklyReports.Where(p => userIdsInCompany.Contains(p.AspNetUserId) && p.Day > begTime).ToList();
                var sessionsCompanyOld = _context.VSessionUserWeeklyReports.Where(p => userIdsInCompany.Contains(p.AspNetUserId) && p.Day <= begTime).ToList();
                var dialoguesCompany = _context.VWeeklyUserReports.Where(p => userIdsInCompany.Contains(p.AspNetUserId) && p.Day > begTime).ToList();
                var dialoguesCompanyOld = _context.VWeeklyUserReports.Where(p => userIdsInCompany.Contains(p.AspNetUserId) && p.Day <= begTime).ToList();
                //-----for User---
                var userSessions = sessionsCompany.Where(p => p.AspNetUserId == userId).ToList();
                var userSessionsOld = sessionsCompanyOld.Where(p => p.AspNetUserId == userId).ToList();
                var userDialogues = dialoguesCompany.Where(p => p.AspNetUserId == userId).ToList();
                var userDialoguesOld = dialoguesCompanyOld.Where(p => p.AspNetUserId == userId).ToList();

                var usersInCorporation = userIdsInCorporation != null? userIdsInCorporation.Count() : 0;
                var usersInCompany = userIdsInCompany.Count();

                //----satisfaction--------
                var Satisfaction = new UserWeeklyInfo(usersInCorporation, usersInCompany)
                {
                    TotalAvg = _dbOperation.TotalAvg(userDialogues, "Satisfaction"),
                    AvgPerDay = _dbOperation.AvgPerDay(userDialogues, "Satisfaction"),
                    OfficeRating = _dbOperation.OfficeRatingSatisfactionPlace(dialoguesCompany, userId),
                    CorporationRating = _dbOperation.OfficeRatingSatisfactionPlace(dialoguesCorporation, userId)
                };
                var TotalAvgOld = _dbOperation.TotalAvg(userDialoguesOld, "Satisfaction");
                int? OfficeRatingOld = _dbOperation.OfficeRatingSatisfactionPlace(dialoguesCompanyOld, userId);
                Satisfaction.Dynamic = Satisfaction.TotalAvg - TotalAvgOld;
                Satisfaction.OfficeRatingChanges = Satisfaction.OfficeRating - OfficeRatingOld;

                int? CorporationRatingOld = _dbOperation.OfficeRatingSatisfactionPlace(dialoguesCorporationOld, userId);
                Satisfaction.CorporationRatingChanges = Satisfaction.CorporationRating - CorporationRatingOld;
                jsonToReturn["Satisfaction"] = Satisfaction;

                //   //----positiveEmotions--------
                var PositiveEmotions = new UserWeeklyInfo(usersInCorporation, usersInCompany)
                {
                    TotalAvg = _dbOperation.TotalAvg(userDialogues, "PositiveEmotions"),
                    AvgPerDay = _dbOperation.AvgPerDay(userDialogues, "PositiveEmotions"),
                    OfficeRating = _dbOperation.OfficeRatingPositiveEmotPlace(dialoguesCompany, userId),
                    CorporationRating = _dbOperation.OfficeRatingPositiveEmotPlace(dialoguesCorporation, userId)
                };
                TotalAvgOld = _dbOperation.TotalAvg(userDialoguesOld, "PositiveEmotions");
                OfficeRatingOld = _dbOperation.OfficeRatingPositiveEmotPlace(dialoguesCompanyOld, userId);
                PositiveEmotions.Dynamic = PositiveEmotions.TotalAvg - TotalAvgOld;
                PositiveEmotions.OfficeRatingChanges = PositiveEmotions.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _dbOperation.OfficeRatingPositiveEmotPlace(dialoguesCorporationOld, userId);
                PositiveEmotions.CorporationRatingChanges = PositiveEmotions.CorporationRating - CorporationRatingOld;
                jsonToReturn["PositiveEmotions"] = PositiveEmotions;

                //    //----positiveIntonations--------
                var PositiveIntonations = new UserWeeklyInfo(usersInCorporation, usersInCompany)
                {
                    TotalAvg = _dbOperation.TotalAvg(userDialogues, "PositiveTone"),
                    AvgPerDay = _dbOperation.AvgPerDay(userDialogues, "PositiveTone"),
                    OfficeRating = _dbOperation.OfficeRatingPositiveIntonationPlace(dialoguesCompany, userId),
                    CorporationRating = _dbOperation.OfficeRatingPositiveIntonationPlace(dialoguesCorporation, userId)
                };
                TotalAvgOld = _dbOperation.TotalAvg(userDialoguesOld, "PositiveTone");
                OfficeRatingOld = _dbOperation.OfficeRatingPositiveIntonationPlace(dialoguesCompanyOld, userId);
                PositiveIntonations.Dynamic = PositiveIntonations.TotalAvg - TotalAvgOld;
                PositiveIntonations.OfficeRatingChanges = PositiveIntonations.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _dbOperation.OfficeRatingPositiveIntonationPlace(dialoguesCorporationOld, userId);
                PositiveIntonations.CorporationRatingChanges = PositiveIntonations.CorporationRating - CorporationRatingOld;
                jsonToReturn["PositiveIntonations"] = PositiveIntonations;

                //    //----speechEmotivity--------
                var SpeechEmotivity = new UserWeeklyInfo(usersInCorporation, usersInCompany)
                {
                    TotalAvg = _dbOperation.TotalAvg(userDialogues, "SpeekEmotions"),
                    AvgPerDay = _dbOperation.AvgPerDay(userDialogues, "SpeekEmotions"),
                    OfficeRating = _dbOperation.OfficeRatingSpeechEmotPlace(dialoguesCompany, userId),
                    CorporationRating = _dbOperation.OfficeRatingSpeechEmotPlace(dialoguesCorporation, userId)
                };
                TotalAvgOld = _dbOperation.TotalAvg(userDialoguesOld, "SpeekEmotions");
                OfficeRatingOld = _dbOperation.OfficeRatingSpeechEmotPlace(dialoguesCompanyOld, userId);
                SpeechEmotivity.Dynamic = SpeechEmotivity.TotalAvg - TotalAvgOld;
                SpeechEmotivity.OfficeRatingChanges = SpeechEmotivity.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _dbOperation.OfficeRatingSpeechEmotPlace(dialoguesCorporationOld, userId);
                SpeechEmotivity.CorporationRatingChanges = SpeechEmotivity.CorporationRating - CorporationRatingOld;
                jsonToReturn["SpeechEmotivity"] = SpeechEmotivity;

                //    //----workload--------
                //---number of dialogues per day--
                var NumberOfDialogues = new UserWeeklyInfo(usersInCorporation, usersInCompany)
                {
                    TotalAvg = userDialogues.Sum(p => p.Dialogues),
                    AvgPerDay = _dbOperation.AvgNumberOfDialoguesPerDay(userDialogues),
                    OfficeRating = _dbOperation.OfficeRatingDialoguesAmount(dialoguesCompany, userId),
                    CorporationRating = _dbOperation.OfficeRatingDialoguesAmount(dialoguesCorporation, userId)
                };
                TotalAvgOld = userDialoguesOld.Sum(p => p.Dialogues);
                OfficeRatingOld = _dbOperation.OfficeRatingDialoguesAmount(dialoguesCompanyOld, userId);
                NumberOfDialogues.Dynamic = NumberOfDialogues.TotalAvg - TotalAvgOld;
                NumberOfDialogues.OfficeRatingChanges = NumberOfDialogues.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _dbOperation.OfficeRatingDialoguesAmount(dialoguesCorporationOld, userId);
                NumberOfDialogues.CorporationRatingChanges = NumberOfDialogues.CorporationRating - CorporationRatingOld;
                jsonToReturn["NumberOfDialogues"] = NumberOfDialogues;

                //---working day ----
                var WorkingHours = new UserWeeklyInfo(usersInCorporation, usersInCompany)
                {
                    TotalAvg = userSessions.Sum(p => p.SessionsHours),
                    AvgPerDay = _dbOperation.AvgWorkingHoursPerDay( userSessions),
                    OfficeRating = _dbOperation.OfficeRatingWorkingHours(sessionsCompany, userId),
                    CorporationRating = _dbOperation.OfficeRatingWorkingHours(sessionsCorporation, userId)
                };
                TotalAvgOld = userSessionsOld.Sum(p => p.SessionsHours);
                OfficeRatingOld = _dbOperation.OfficeRatingWorkingHours(sessionsCompanyOld, userId);
                WorkingHours.Dynamic = WorkingHours.TotalAvg - TotalAvgOld;
                WorkingHours.OfficeRatingChanges = WorkingHours.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _dbOperation.OfficeRatingWorkingHours(sessionsCorporationOld, userId);
                WorkingHours.CorporationRatingChanges = WorkingHours.CorporationRating - CorporationRatingOld;
                jsonToReturn["WorkingHours_SessionsTotal"] = WorkingHours;

                //----time of clients work---
                var AvgDialogueTime = new UserWeeklyInfo(usersInCorporation, usersInCompany)
                {
                    TotalAvg = TimeSpan.FromHours(_dbOperation.AvgDialogueTimeTotal(userDialogues)).TotalMinutes,
                    AvgPerDay = _dbOperation.AvgDialogueTimePerDay(userDialogues),
                    OfficeRating = _dbOperation.OfficeRatingDialogueTime(dialoguesCompany, userId),
                    CorporationRating = _dbOperation.OfficeRatingDialogueTime(dialoguesCorporation, userId)
                };
                TotalAvgOld = _dbOperation.AvgDialogueTimeTotal(userDialoguesOld);
                OfficeRatingOld = _dbOperation.OfficeRatingDialogueTime(dialoguesCompanyOld, userId);
                AvgDialogueTime.Dynamic = AvgDialogueTime.TotalAvg - TimeSpan.FromHours((double)(TotalAvgOld)).TotalMinutes;
                AvgDialogueTime.OfficeRatingChanges = AvgDialogueTime.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _dbOperation.OfficeRatingDialogueTime(dialoguesCorporationOld, userId);
                AvgDialogueTime.CorporationRatingChanges = AvgDialogueTime.CorporationRating - CorporationRatingOld;
                jsonToReturn["AvgDialogueTime"] = AvgDialogueTime;
                //---load---
                var Workload = new UserWeeklyInfo(usersInCorporation, usersInCompany)
                {
                    TotalAvg = 100 * _dbOperation.WorkloadTotal(userDialogues, userSessions),
                    AvgPerDay = _dbOperation.AvgWorkloadPerDay(userDialogues, userSessions),
                    OfficeRating = _dbOperation.OfficeRatingWorkload(dialoguesCompany, sessionsCompany, userId),
                    CorporationRating = _dbOperation.OfficeRatingWorkload(dialoguesCorporation, sessionsCorporation, userId)
                };
                TotalAvgOld =  100 * _dbOperation.WorkloadTotal(userDialoguesOld, userSessionsOld);
                OfficeRatingOld = _dbOperation.OfficeRatingWorkload(dialoguesCompanyOld, sessionsCompanyOld, userId);
                Workload.Dynamic = Workload.TotalAvg - TotalAvgOld;
                Workload.OfficeRatingChanges = Workload.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _dbOperation.OfficeRatingWorkload(dialoguesCorporationOld, sessionsCorporationOld, userId);
                Workload.CorporationRatingChanges = Workload.CorporationRating - CorporationRatingOld;
                jsonToReturn["Workload"] = Workload;

                //---standarts---            
                var CrossPhrase = new UserWeeklyInfo(usersInCorporation, usersInCompany)
                {
                    TotalAvg = _dbOperation.PhraseTotalAvg(userDialogues, "CrossDialogues"),
                    AvgPerDay = _dbOperation.PhraseAvgPerDay(userDialogues, "CrossDialogues"),// userDialogues.ToDictionary(x => x.Day, i => (double)i.CrossDialogues / i.Dialogues),
                    OfficeRating = _dbOperation.OfficeRating(dialoguesCompany, userId, "CrossDialogues"),
                    CorporationRating = _dbOperation.OfficeRating(dialoguesCorporation, userId, "CrossDialogues")
                };
                TotalAvgOld = _dbOperation.PhraseTotalAvg(userDialoguesOld, "CrossDialogues");
                OfficeRatingOld = _dbOperation.OfficeRating(dialoguesCompanyOld, userId, "CrossDialogues");
                CrossPhrase.Dynamic = CrossPhrase.TotalAvg - TotalAvgOld;
                CrossPhrase.OfficeRatingChanges = CrossPhrase.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _dbOperation.OfficeRating(dialoguesCorporationOld, userId, "CrossDialogues");
                CrossPhrase.CorporationRatingChanges = CrossPhrase.CorporationRating - CorporationRatingOld;
                jsonToReturn["CrossPhrase"] = CrossPhrase;

                var AlertPhrase = new UserWeeklyInfo(usersInCorporation, usersInCompany)
                {
                    TotalAvg = _dbOperation.PhraseTotalAvg(userDialogues, "AlertDialogues"),
                    AvgPerDay =  _dbOperation.PhraseAvgPerDay(userDialogues, "AlertDialogues"),
                    OfficeRating = _dbOperation.OfficeRating(dialoguesCompany, userId, "AlertDialogues"),
                    CorporationRating = _dbOperation.OfficeRating(dialoguesCorporation, userId, "AlertDialogues")
                };
                TotalAvgOld = _dbOperation.PhraseTotalAvg(userDialoguesOld, "AlertDialogues");
                OfficeRatingOld = _dbOperation.OfficeRating(dialoguesCompanyOld, userId, "AlertDialogues");
                AlertPhrase.Dynamic = AlertPhrase.TotalAvg - TotalAvgOld;
                AlertPhrase.OfficeRatingChanges = AlertPhrase.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _dbOperation.OfficeRating(dialoguesCorporationOld, userId, "AlertDialogues");
                AlertPhrase.CorporationRatingChanges = AlertPhrase.CorporationRating - CorporationRatingOld;
                jsonToReturn["AlertPhrase"] = AlertPhrase;

                var LoyaltyPhrase = new UserWeeklyInfo(usersInCorporation, usersInCompany)
                {
                    TotalAvg =  _dbOperation.PhraseTotalAvg(userDialogues, "LoyaltyDialogues"),
                    AvgPerDay =  _dbOperation.PhraseAvgPerDay(userDialogues, "LoyaltyDialogues"),
                    OfficeRating = _dbOperation.OfficeRating(dialoguesCompany, userId, "LoyaltyDialogues"),
                    CorporationRating = _dbOperation.OfficeRating(dialoguesCorporation, userId, "LoyaltyDialogues")
                };
                TotalAvgOld = _dbOperation.PhraseTotalAvg(userDialoguesOld, "LoyaltyDialogues");
                OfficeRatingOld = _dbOperation.OfficeRating(dialoguesCompanyOld, userId, "LoyaltyDialogues");
                LoyaltyPhrase.Dynamic = LoyaltyPhrase.TotalAvg - TotalAvgOld;
                LoyaltyPhrase.OfficeRatingChanges = LoyaltyPhrase.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _dbOperation.OfficeRating(dialoguesCorporationOld, userId, "LoyaltyDialogues");
                LoyaltyPhrase.CorporationRatingChanges = LoyaltyPhrase.CorporationRating - CorporationRatingOld;
                jsonToReturn["LoyaltyPhrase"] = LoyaltyPhrase;

                var NecessaryPhrase = new UserWeeklyInfo(usersInCorporation, usersInCompany)
                {
                    TotalAvg =  _dbOperation.PhraseTotalAvg(userDialogues, "NecessaryDialogues"),
                   AvgPerDay = _dbOperation.PhraseAvgPerDay(userDialogues, "NecessaryDialogues"),
                    OfficeRating = _dbOperation.OfficeRating(dialoguesCompany, userId, "NecessaryDialogues"),
                    CorporationRating = _dbOperation.OfficeRating(dialoguesCorporation, userId, "NecessaryDialogues")
                };
                TotalAvgOld = _dbOperation.PhraseTotalAvg(userDialoguesOld, "NecessaryDialogues");
                OfficeRatingOld = _dbOperation.OfficeRating(dialoguesCompanyOld, userId, "NecessaryDialogues");
                NecessaryPhrase.Dynamic = NecessaryPhrase.TotalAvg - TotalAvgOld;
                NecessaryPhrase.OfficeRatingChanges = NecessaryPhrase.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _dbOperation.OfficeRating(dialoguesCorporationOld, userId, "NecessaryDialogues");
                NecessaryPhrase.CorporationRatingChanges = NecessaryPhrase.CorporationRating - CorporationRatingOld;
                jsonToReturn["NecessaryPhrase"] = NecessaryPhrase;

                var FillersPhrase = new UserWeeklyInfo(usersInCorporation, usersInCompany)
                {
                    TotalAvg = _dbOperation.PhraseTotalAvg(userDialoguesOld, "FillersDialogues"),
                    AvgPerDay = _dbOperation.PhraseAvgPerDay(userDialogues, "FillersDialogues"),
                    OfficeRating = _dbOperation.OfficeRating(dialoguesCompany, userId, "FillersDialogues"),
                    CorporationRating = _dbOperation.OfficeRating(dialoguesCorporation, userId, "FillersDialogues")
                };
                TotalAvgOld = _dbOperation.PhraseTotalAvg(userDialoguesOld, "FillersDialogues");
                FillersPhrase.Dynamic = FillersPhrase.TotalAvg - TotalAvgOld;

                OfficeRatingOld = _dbOperation.OfficeRating(dialoguesCompanyOld, userId, "FillersDialogues");
                FillersPhrase.OfficeRatingChanges = FillersPhrase.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _dbOperation.OfficeRating(dialoguesCorporationOld, userId, "FillersDialogues");
                FillersPhrase.CorporationRatingChanges = FillersPhrase.CorporationRating - CorporationRatingOld;
                jsonToReturn["FillersPhrase"] = FillersPhrase;

                var RiskPhrase = new UserWeeklyInfo(usersInCorporation, usersInCompany)
                {
                    TotalAvg = _dbOperation.PhraseTotalAvg(userDialoguesOld, "RiskDialogues"),
                    AvgPerDay = _dbOperation.PhraseAvgPerDay(userDialogues, "RiskDialogues"),
                    OfficeRating = _dbOperation.OfficeRating(dialoguesCompany, userId, "RiskDialogues"),
                    CorporationRating = _dbOperation.OfficeRating(dialoguesCorporation, userId, "RiskDialogues")
                };
                TotalAvgOld = _dbOperation.PhraseTotalAvg(userDialoguesOld, "RiskDialogues");
                RiskPhrase.Dynamic = RiskPhrase.TotalAvg - TotalAvgOld;

                OfficeRatingOld = _dbOperation.OfficeRating(dialoguesCompanyOld, userId, "RiskDialogues");
                RiskPhrase.OfficeRatingChanges = RiskPhrase.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _dbOperation.OfficeRating(dialoguesCorporationOld, userId, "RiskDialogues");
                RiskPhrase.CorporationRatingChanges = RiskPhrase.CorporationRating - CorporationRatingOld;
                jsonToReturn["RiskPhrase"] = RiskPhrase;

                jsonToReturn["corporationId"] = corporationId;
                return Ok(jsonToReturn);
            }
            catch (Exception e)
            {
//                _log.Fatal($"Exception occurred {e}");
                return BadRequest(e);
            }
        }

    }
}