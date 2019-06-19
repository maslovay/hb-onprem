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

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticWeeklyReportController : Controller
    {
      private readonly IConfiguration _config;        
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
        private readonly DBOperations _dbOperation;
        private readonly RequestFilters _requestFilters;
        private readonly ElasticClient _log;

        public AnalyticWeeklyReportController(
            IConfiguration config,
            ILoginService loginService,
            RecordsContext context,
            DBOperations dbOperation,
            RequestFilters requestFilters,
            ElasticClient log
            )
        {
            _config = config;
            _loginService = loginService;
            _context = context;
            _dbOperation = dbOperation;
            _requestFilters = requestFilters;
            _log = log;
        }

        [HttpGet("User")]
        public IActionResult RatingProgress([FromHeader] string Authorization)
        {
            try
            {
                _log.Info("AnalyticWeeklyReport/Progress started");
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var jsonToReturn = new Dictionary<string, object>();

                var userId =  Guid.Parse(userClaims["applicationUserId"]); //Guid.Parse("010039d5-895b-47ad-bd38-eb28685ab9aa");//
                var companyId =   Guid.Parse(userClaims["companyId"]); //Guid.Parse("72685b5f-d22a-4a72-9799-30af486ada48");//
                var corporationId = _context.Companys.Where(p => p.CompanyId == companyId).FirstOrDefault()?.CorporationId;
                var userIdsInCorporation = _context.Companys
                        .Include(p => p.ApplicationUser)
                        .Where(p => p.CorporationId == corporationId).SelectMany(p => p.ApplicationUser.Select(u => u.Id)).ToList();
                var userIdsInCompany = _context.ApplicationUsers
                        .Where(p => p.CompanyId == companyId).Select(u => u.Id).ToList();

                  //----ALL FOR WEEK Corporation---
                var sessionsCorporation =  _context.VSessionWeeklyReports.Where(p => userIdsInCorporation.Contains(p.AspNetUserId)).ToList();
                var sessionsCorporationOld = _context.VSessionWeeklyReportsOld.Where(p => userIdsInCorporation.Contains(p.AspNetUserId)).ToList();
                var dialoguesCorporation = _context.VWeeklyReports.Where(p => userIdsInCorporation.Contains(p.AspNetUserId)).ToList();
                var dialoguesCorporationOld =   _context.VWeeklyReportsOld.Where(p => userIdsInCorporation.Contains(p.AspNetUserId)).ToList();
                  //----ALL FOR WEEK Company---
                var sessionsCompany =  _context.VSessionWeeklyReports.Where(p => userIdsInCompany.Contains(p.AspNetUserId)).ToList();
                var sessionsCompanyOld = _context.VSessionWeeklyReportsOld.Where(p => userIdsInCompany.Contains(p.AspNetUserId)).ToList();
                var dialoguesCompany = _context.VWeeklyReports.Where(p => userIdsInCompany.Contains(p.AspNetUserId)).ToList();
                var dialoguesCompanyOld =   _context.VWeeklyReportsOld.Where(p => userIdsInCompany.Contains(p.AspNetUserId)).ToList();
                //-----for User---
                var userSessions = sessionsCompany.Where(p => p.AspNetUserId == userId).ToList();
                var userSessionsOld = sessionsCompanyOld.Where(p => p.AspNetUserId == userId).ToList();
                var userDialogues = dialoguesCompany.Where(p => p.AspNetUserId == userId).ToList();
                var userDialoguesOld = dialoguesCompanyOld.Where(p => p.AspNetUserId == userId).ToList();



                //----satisfaction--------
                var Satisfaction = new UserWeeklyInfo
                {
                    TotalAvg = userDialogues.Sum(p => p.Satisfaction) / userDialogues.Count(),
                    AvgPerDay = userDialogues.ToDictionary(x => x.Day, i => i.Satisfaction),
                    OfficeRating = _dbOperation.OfficeRatingSatisfactionPlace(dialoguesCompany, userId),
                    CorporationRating =  _dbOperation.OfficeRatingSatisfactionPlace(dialoguesCorporation, userId)
                };
                var TotalAvgOld = userDialoguesOld.Sum(p => p.Satisfaction) / userDialoguesOld.Count();
                int? OfficeRatingOld = _dbOperation.OfficeRatingSatisfactionPlace(dialoguesCompanyOld, userId);                    
                Satisfaction.Dynamic = Satisfaction.TotalAvg - TotalAvgOld;
                Satisfaction.OfficeRatingChanges  = Satisfaction.OfficeRating - OfficeRatingOld;

                int? CorporationRatingOld = _dbOperation.OfficeRatingSatisfactionPlace(dialoguesCorporationOld, userId);
                Satisfaction.CorporationRatingChanges  = Satisfaction.CorporationRating - CorporationRatingOld;
                jsonToReturn["Satisfaction"] = Satisfaction;

                //   //----positiveEmotions--------
                var PositiveEmotions = new UserWeeklyInfo
                {
                    TotalAvg = userDialogues.Sum(p => p.PositiveEmotions) / userDialogues.Count(),
                    AvgPerDay = userDialogues.ToDictionary(x => x.Day, i => i.PositiveEmotions),
                    OfficeRating = _dbOperation.OfficeRatingPositiveEmotPlace(dialoguesCompany, userId),
                    CorporationRating =  _dbOperation.OfficeRatingPositiveEmotPlace(dialoguesCorporation, userId)
                };
                TotalAvgOld = userDialoguesOld.Sum(p => p.PositiveEmotions) / userDialoguesOld.Count();
                OfficeRatingOld = _dbOperation.OfficeRatingPositiveEmotPlace(dialoguesCompanyOld, userId);
                PositiveEmotions.Dynamic  = PositiveEmotions.TotalAvg - TotalAvgOld;
                PositiveEmotions.OfficeRatingChanges = PositiveEmotions.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _dbOperation.OfficeRatingPositiveEmotPlace(dialoguesCorporationOld, userId);
                PositiveEmotions.CorporationRatingChanges  = PositiveEmotions.CorporationRating - CorporationRatingOld;
                jsonToReturn["PositiveEmotions"] = PositiveEmotions;

                //    //----positiveIntonations--------
                var PositiveIntonations = new UserWeeklyInfo
                {
                    TotalAvg = userDialogues.Sum(p => p.PositiveTone) / userDialogues.Count(),
                    AvgPerDay = userDialogues.ToDictionary(x => x.Day, i => i.PositiveTone),
                    OfficeRating = _dbOperation.OfficeRatingPositiveIntonationPlace(dialoguesCompany, userId),
                    CorporationRating =  _dbOperation.OfficeRatingPositiveIntonationPlace(dialoguesCorporation, userId)
                };
                TotalAvgOld = userDialoguesOld.Sum(p => p.PositiveTone) / userDialoguesOld.Count();
                OfficeRatingOld = _dbOperation.OfficeRatingPositiveIntonationPlace(dialoguesCompanyOld, userId);
                PositiveIntonations.Dynamic = PositiveIntonations.TotalAvg - TotalAvgOld;
                PositiveIntonations.OfficeRatingChanges  = PositiveIntonations.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _dbOperation.OfficeRatingPositiveIntonationPlace(dialoguesCorporationOld, userId);
                PositiveIntonations.CorporationRatingChanges  = PositiveIntonations.CorporationRating - CorporationRatingOld;
                jsonToReturn["PositiveIntonations"] = PositiveIntonations;

                //    //----speechEmotivity--------
                var SpeechEmotivity = new UserWeeklyInfo
                {
                    TotalAvg = userDialogues.Sum(p => (double?) p.SpeekEmotions) / userDialogues.Count(),
                    AvgPerDay = userDialogues.ToDictionary(x => x.Day, i => i.SpeekEmotions),
                    OfficeRating = _dbOperation.OfficeRatingSpeechEmotPlace(dialoguesCompany, userId),
                    CorporationRating =  _dbOperation.OfficeRatingSpeechEmotPlace(dialoguesCorporation, userId)
                };
                TotalAvgOld = userDialoguesOld.Sum(p => (double?) p.SpeekEmotions) / userDialoguesOld.Count();
                OfficeRatingOld = _dbOperation.OfficeRatingSpeechEmotPlace(dialoguesCompanyOld, userId);
                SpeechEmotivity.Dynamic = SpeechEmotivity.TotalAvg - TotalAvgOld;
                SpeechEmotivity.OfficeRatingChanges  = SpeechEmotivity.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _dbOperation.OfficeRatingSpeechEmotPlace(dialoguesCorporationOld, userId);
                SpeechEmotivity.CorporationRatingChanges  = SpeechEmotivity.CorporationRating - CorporationRatingOld;
                jsonToReturn["SpeechEmotivity"] = SpeechEmotivity;

                //    //----workload--------
                var Workload = new UserWeeklyInfo
                {
                    TotalAvg = userDialogues.Sum(p => p.DialogueHours) / userSessions.Sum(p => p.SessionsHours),
                    AvgPerDay = _dbOperation.AvgWorkloadPerDay(userDialogues, userSessions),
                    OfficeRating = _dbOperation.OfficeRatingWorkload(dialoguesCompany, sessionsCompany, userId),
                    CorporationRating = _dbOperation.OfficeRatingWorkload(dialoguesCorporation, sessionsCorporation, userId)
                };
                TotalAvgOld = userDialoguesOld.Sum(p => p.DialogueHours) / sessionsCompanyOld.Sum(p => p.SessionsHours);
                OfficeRatingOld = _dbOperation.OfficeRatingWorkload(dialoguesCompanyOld, sessionsCompanyOld, userId);
                Workload.Dynamic = Workload.TotalAvg - TotalAvgOld;
                Workload.OfficeRatingChanges  = Workload.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _dbOperation.OfficeRatingWorkload(dialoguesCorporationOld, sessionsCorporationOld, userId);
                Workload.CorporationRatingChanges  = Workload.CorporationRating - CorporationRatingOld;
                jsonToReturn["Workload"] = Workload;

                var WorkingHours = new UserWeeklyInfo
                {
                    TotalAvg = userSessions.Sum(p => p.SessionsHours),
                    AvgPerDay = userSessions.ToDictionary(x => x.Day, i => (double?)i.SessionsHours),
                    OfficeRating = _dbOperation.OfficeRatingWorkingHours(sessionsCompany, userId),
                    CorporationRating = _dbOperation.OfficeRatingWorkingHours(sessionsCorporation, userId)
                };
                TotalAvgOld = userSessionsOld.Sum(p => p.SessionsHours);
                OfficeRatingOld = _dbOperation.OfficeRatingWorkingHours(sessionsCompanyOld, userId);
                WorkingHours.Dynamic = WorkingHours.TotalAvg - TotalAvgOld;
                WorkingHours.OfficeRatingChanges  = WorkingHours.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _dbOperation.OfficeRatingWorkingHours(sessionsCorporationOld, userId);
                WorkingHours.CorporationRatingChanges  = WorkingHours.CorporationRating - CorporationRatingOld;
                jsonToReturn["WorkingHours_SessionsTotal"] = WorkingHours;

                var AvgDialogueTime = new UserWeeklyInfo
                {
                    TotalAvg = userDialogues.Sum(p => p.DialogueHours) / userDialogues.Sum(p => p.Dialogues),
                    AvgPerDay = userDialogues.ToDictionary(x => x.Day, i => (double?)i.DialogueHours / i.Dialogues),
                    OfficeRating = _dbOperation.OfficeRatingDialogueTime(dialoguesCompany, userId),
                    CorporationRating = _dbOperation.OfficeRatingDialogueTime(dialoguesCorporation, userId)
                };
                TotalAvgOld = userDialoguesOld.Sum(p => p.DialogueHours) / userDialoguesOld.Sum(p => p.Dialogues);
                OfficeRatingOld = _dbOperation.OfficeRatingDialogueTime(dialoguesCompanyOld, userId);
                AvgDialogueTime.Dynamic = AvgDialogueTime.TotalAvg - TotalAvgOld;
                AvgDialogueTime.OfficeRatingChanges  = AvgDialogueTime.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _dbOperation.OfficeRatingDialogueTime(dialoguesCorporationOld, userId);
                AvgDialogueTime.CorporationRatingChanges  = AvgDialogueTime.CorporationRating - CorporationRatingOld;
                jsonToReturn["AvgDialogueTime"] = AvgDialogueTime;

                var TotalDialogueTime = new UserWeeklyInfo
                {
                    TotalAvg = userDialogues.Sum(p => p.DialogueHours) ,
                    AvgPerDay = userDialogues.ToDictionary(x => x.Day, i => (double?)i.DialogueHours),
                    OfficeRating = _dbOperation.OfficeRatingDialogueTimeTotal(dialoguesCompany, userId),
                    CorporationRating = _dbOperation.OfficeRatingDialogueTimeTotal(dialoguesCorporation, userId)
                };
                TotalAvgOld = userDialoguesOld.Sum(p => p.DialogueHours);
                OfficeRatingOld = _dbOperation.OfficeRatingDialogueTimeTotal(dialoguesCompanyOld, userId);
                TotalDialogueTime.Dynamic = TotalDialogueTime.TotalAvg - TotalAvgOld;
                TotalDialogueTime.OfficeRatingChanges  = TotalDialogueTime.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _dbOperation.OfficeRatingDialogueTimeTotal(dialoguesCorporationOld, userId);
                TotalDialogueTime.CorporationRatingChanges  = TotalDialogueTime.CorporationRating - CorporationRatingOld;
                jsonToReturn["TotalDialogueTime"] = TotalDialogueTime;

                var NumberOfDialogues  = new UserWeeklyInfo
                {
                    TotalAvg = userDialogues.Sum(p => p.Dialogues),
                    AvgPerDay = userDialogues.ToDictionary(x => x.Day, i => (double?)i.Dialogues),
                    OfficeRating = _dbOperation.OfficeRatingDialoguesAmount(dialoguesCompany, userId),
                    CorporationRating = _dbOperation.OfficeRatingDialoguesAmount(dialoguesCorporation, userId)
                };
                TotalAvgOld = userDialoguesOld.Sum(p => p.Dialogues);
                OfficeRatingOld = _dbOperation.OfficeRatingDialoguesAmount(dialoguesCompanyOld, userId);
                NumberOfDialogues.Dynamic = NumberOfDialogues.TotalAvg - TotalAvgOld;
                NumberOfDialogues.OfficeRatingChanges  = NumberOfDialogues.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _dbOperation.OfficeRatingDialoguesAmount(dialoguesCorporationOld, userId);
                NumberOfDialogues.CorporationRatingChanges  = NumberOfDialogues.CorporationRating - CorporationRatingOld;
                jsonToReturn["NumberOfDialogues"] = NumberOfDialogues; 
                
                var CrossPhrase = new UserWeeklyInfo
                {
                    TotalAvg = userDialogues.Sum(p => (double?)p.CrossDialogues) / userDialogues.Sum(p => (double?)p.Dialogues),
                    AvgPerDay = userDialogues.ToDictionary(x => x.Day, i => (double?)i.CrossDialogues / i.Dialogues),
                    OfficeRating = _dbOperation.OfficeRatingCross(dialoguesCompany, userId),
                    CorporationRating = _dbOperation.OfficeRatingCross(dialoguesCorporation, userId)
                };
                TotalAvgOld = userDialoguesOld.Sum(p => (double?)p.CrossDialogues) / userDialoguesOld.Sum(p => (double?)p.Dialogues);
                OfficeRatingOld = _dbOperation.OfficeRatingCross(dialoguesCompanyOld, userId);
                CrossPhrase.Dynamic = CrossPhrase.TotalAvg - TotalAvgOld;
                CrossPhrase.OfficeRatingChanges  = CrossPhrase.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _dbOperation.OfficeRatingCross(dialoguesCorporationOld, userId);
                CrossPhrase.CorporationRatingChanges  = CrossPhrase.CorporationRating - CorporationRatingOld;
                jsonToReturn["CrossPhrase"] = CrossPhrase;

                var AlertPhrase = new UserWeeklyInfo
                {
                    TotalAvg = userDialogues.Sum(p => (double?)p.AlertDialogues) / userDialogues.Sum(p => (double?)p.Dialogues),
                    AvgPerDay = userDialogues.ToDictionary(x => x.Day, i => (double?)i.AlertDialogues / i.Dialogues),
                    OfficeRating = _dbOperation.OfficeRatingAlert(dialoguesCompany, userId),
                    CorporationRating = _dbOperation.OfficeRatingAlert(dialoguesCorporation, userId)
                };
                TotalAvgOld = userDialoguesOld.Sum(p => (double?)p.AlertDialogues) / userDialoguesOld.Sum(p => (double?)p.Dialogues);
                OfficeRatingOld = _dbOperation.OfficeRatingAlert(dialoguesCompanyOld, userId);
                AlertPhrase.Dynamic = AlertPhrase.TotalAvg - TotalAvgOld;
                AlertPhrase.OfficeRatingChanges  = AlertPhrase.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _dbOperation.OfficeRatingAlert(dialoguesCorporationOld, userId);
                AlertPhrase.CorporationRatingChanges  = AlertPhrase.CorporationRating - CorporationRatingOld;
                jsonToReturn["AlertPhrase"] = AlertPhrase;

                var LoyaltyPhrase = new UserWeeklyInfo
                {
                    TotalAvg = userDialogues.Sum(p => (double?)p.LoyaltyDialogues) / userDialogues.Sum(p => (double?)p.Dialogues),
                    AvgPerDay = userDialogues.ToDictionary(x => x.Day, i => (double?)i.LoyaltyDialogues / i.Dialogues),
                    OfficeRating = _dbOperation.OfficeRatingLoyalty(dialoguesCompany, userId),
                    CorporationRating = _dbOperation.OfficeRatingLoyalty(dialoguesCorporation, userId)
                };
                TotalAvgOld = userDialoguesOld.Sum(p => (double?)p.LoyaltyDialogues) / userDialoguesOld.Sum(p => (double?)p.Dialogues);
                OfficeRatingOld = _dbOperation.OfficeRatingLoyalty(dialoguesCompanyOld, userId);
                LoyaltyPhrase.Dynamic = LoyaltyPhrase.TotalAvg - TotalAvgOld;
                LoyaltyPhrase.OfficeRatingChanges  = LoyaltyPhrase.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _dbOperation.OfficeRatingLoyalty(dialoguesCorporationOld, userId);
                LoyaltyPhrase.CorporationRatingChanges  = LoyaltyPhrase.CorporationRating - CorporationRatingOld;
                jsonToReturn["LoyaltyPhrase"] = LoyaltyPhrase;

                var NecessaryPhrase = new UserWeeklyInfo
                {
                    TotalAvg = userDialogues.Sum(p => (double?)p.NecessaryDialogues) / userDialogues.Sum(p => (double?)p.Dialogues),
                    AvgPerDay = userDialogues.ToDictionary(x => x.Day, i => (double?)i.NecessaryDialogues / i.Dialogues),
                    OfficeRating = _dbOperation.OfficeRatingNecessary(dialoguesCompany, userId),
                    CorporationRating = _dbOperation.OfficeRatingNecessary(dialoguesCorporation, userId)
                };
                TotalAvgOld = userDialoguesOld.Sum(p => (double?)p.NecessaryDialogues) / userDialoguesOld.Sum(p => (double?)p.Dialogues);
                OfficeRatingOld = _dbOperation.OfficeRatingNecessary(dialoguesCompanyOld, userId);
                NecessaryPhrase.Dynamic = NecessaryPhrase.TotalAvg - TotalAvgOld;
                NecessaryPhrase.OfficeRatingChanges  = NecessaryPhrase.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _dbOperation.OfficeRatingNecessary(dialoguesCorporationOld, userId);
                NecessaryPhrase.CorporationRatingChanges  = NecessaryPhrase.CorporationRating - CorporationRatingOld;
                jsonToReturn["NecessaryPhrase"] = NecessaryPhrase;

                var FillersPhrase = new UserWeeklyInfo
                {
                    TotalAvg = userDialogues.Sum(p => (double?)p.FillersDialogues) / userDialogues.Sum(p => (double?)p.Dialogues),
                    AvgPerDay = userDialogues.ToDictionary(x => x.Day, i => (double?)i.FillersDialogues / i.Dialogues),
                    OfficeRating = _dbOperation.OfficeRatingFillers(dialoguesCompany, userId),
                    CorporationRating = _dbOperation.OfficeRatingFillers(dialoguesCorporation, userId)
                };
                TotalAvgOld = userDialoguesOld.Sum(p => (double?)p.FillersDialogues) / userDialoguesOld.Sum(p => (double?)p.Dialogues);
                FillersPhrase.Dynamic = FillersPhrase.TotalAvg - TotalAvgOld;

                OfficeRatingOld = _dbOperation.OfficeRatingFillers(dialoguesCompanyOld, userId);
                FillersPhrase.OfficeRatingChanges  = FillersPhrase.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _dbOperation.OfficeRatingFillers(dialoguesCorporationOld, userId);
                FillersPhrase.CorporationRatingChanges  = FillersPhrase.CorporationRating - CorporationRatingOld;
                jsonToReturn["FillersPhrase"] = FillersPhrase;


                _log.Info("AnalyticRating/Progress finished");
                //return Ok(JsonConvert.SerializeObject(results));
                return Ok(jsonToReturn);
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occurred {e}");
                return BadRequest(e);
            }
        }
       
    }   
}