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
using UserOperations.AccountModels;
using HBData.Models;
using HBData.Models.AccountViewModels;
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

                  //----ALL FOR WEEK---
                var sessions =  _context.VSessionWeeklyReports.Where(p => userIdsInCorporation.Contains(p.AspNetUserId)).ToList();
                var sessionsOld = _context.VSessionWeeklyReportsOld.Where(p => userIdsInCorporation.Contains(p.AspNetUserId)).ToList();
                var dialogues = _context.VWeeklyReports.Where(p => userIdsInCorporation.Contains(p.AspNetUserId)).ToList();
                var dialoguesOld =   _context.VWeeklyReportsOld.Where(p => userIdsInCorporation.Contains(p.AspNetUserId)).ToList();
                //-----for User---
                var userDialogues = dialogues.Where(p => p.AspNetUserId == userId).ToList();
                var userDialoguesOld = dialoguesOld.Where(p => p.AspNetUserId == userId).ToList();
                var userSessions = sessions.Where(p => p.AspNetUserId == userId).ToList();
                var userSessionsOld = sessionsOld.Where(p => p.AspNetUserId == userId).ToList();



                //----satisfaction--------
                var Satisfaction = new UserWeeklyInfo
                {
                    TotalAvg = userDialogues.Sum(p => p.Satisfaction) / userDialogues.Count(),
                    AvgPerDay = userDialogues.ToDictionary(x => x.Day, i => i.Satisfaction),
                    OfficeRating = _dbOperation.OfficeRatingSatisfactionPlace(dialogues, userId),
                };
                var TotalAvgOld = userDialoguesOld.Sum(p => p.Satisfaction) / userDialoguesOld.Count();
                int? OfficeRatingOld = _dbOperation.OfficeRatingSatisfactionPlace(dialoguesOld, userId);                    
                Satisfaction.Dynamic = Satisfaction.TotalAvg - TotalAvgOld;
                Satisfaction.OfficeRatingChanges  = Satisfaction.OfficeRating - OfficeRatingOld;
                jsonToReturn["Satisfaction"] = Satisfaction;

                //   //----positiveEmotions--------
                var PositiveEmotions = new UserWeeklyInfo
                {
                    TotalAvg = userDialogues.Sum(p => p.PositiveEmotions) / userDialogues.Count(),
                    AvgPerDay = userDialogues.ToDictionary(x => x.Day, i => i.PositiveEmotions),
                    OfficeRating = _dbOperation.OfficeRatingPositiveEmotPlace(dialogues, userId),
                };
                TotalAvgOld = userDialoguesOld.Sum(p => p.PositiveEmotions) / userDialoguesOld.Count();
                OfficeRatingOld = _dbOperation.OfficeRatingPositiveEmotPlace(dialoguesOld, userId);
                PositiveEmotions.Dynamic  = PositiveEmotions.TotalAvg - TotalAvgOld;
                PositiveEmotions.OfficeRatingChanges = PositiveEmotions.OfficeRating - OfficeRatingOld;
                jsonToReturn["PositiveEmotions"] = PositiveEmotions;

                //    //----positiveIntonations--------
                var PositiveIntonations = new UserWeeklyInfo
                {
                    TotalAvg = userDialogues.Sum(p => p.PositiveTone) / userDialogues.Count(),
                    AvgPerDay = userDialogues.ToDictionary(x => x.Day, i => i.PositiveTone),
                    OfficeRating = _dbOperation.OfficeRatingPositiveIntonationPlace(dialogues, userId),
                };
                TotalAvgOld = userDialoguesOld.Sum(p => p.PositiveTone) / userDialoguesOld.Count();
                OfficeRatingOld = _dbOperation.OfficeRatingPositiveIntonationPlace(dialoguesOld, userId);
                PositiveIntonations.Dynamic = PositiveIntonations.TotalAvg - TotalAvgOld;
                PositiveIntonations.OfficeRatingChanges  = PositiveIntonations.OfficeRating - OfficeRatingOld;
                jsonToReturn["PositiveIntonations"] = PositiveIntonations;

                //    //----speechEmotivity--------
                var SpeechEmotivity = new UserWeeklyInfo
                {
                    TotalAvg = userDialogues.Sum(p => (double?) p.SpeekEmotions) / userDialogues.Count(),
                    AvgPerDay = userDialogues.ToDictionary(x => x.Day, i => i.SpeekEmotions),
                    OfficeRating = _dbOperation.OfficeRatingSpeechEmotPlace(dialogues, userId),
                };
                TotalAvgOld = userDialoguesOld.Sum(p => (double?) p.SpeekEmotions) / userDialoguesOld.Count();
                OfficeRatingOld = _dbOperation.OfficeRatingSpeechEmotPlace(dialoguesOld, userId);
                SpeechEmotivity.Dynamic = SpeechEmotivity.TotalAvg - TotalAvgOld;
                SpeechEmotivity.OfficeRatingChanges  = SpeechEmotivity.OfficeRating - OfficeRatingOld;
                jsonToReturn["SpeechEmotivity"] = SpeechEmotivity;

                //    //----workload--------
                var Workload = new UserWeeklyInfo
                {
                    TotalAvg = userDialogues.Sum(p => p.DialogueHours) / sessions.Sum(p => p.SessionsHours),
                    AvgPerDay = _dbOperation.AvgWorkloadPerDay(userDialogues, userSessions),
                    OfficeRating = _dbOperation.OfficeRatingWorkload(dialogues, sessions, userId),
                };
                TotalAvgOld = userDialoguesOld.Sum(p => p.DialogueHours) / sessionsOld.Sum(p => p.SessionsHours);
                OfficeRatingOld = _dbOperation.OfficeRatingWorkload(dialoguesOld, sessionsOld, userId);
                Workload.Dynamic = Workload.TotalAvg - TotalAvgOld;
                Workload.OfficeRatingChanges  = Workload.OfficeRating - OfficeRatingOld;
                jsonToReturn["Workload"] = Workload;

                var WorkingHours = new UserWeeklyInfo
                {
                    TotalAvg = userSessions.Sum(p => p.SessionsHours),
                    AvgPerDay = userSessions.ToDictionary(x => x.Day, i => (double?)i.SessionsHours),
                    OfficeRating = _dbOperation.OfficeRatingWorkingHours(sessions, userId),
                };
                TotalAvgOld = userSessionsOld.Sum(p => p.SessionsHours);
                OfficeRatingOld = _dbOperation.OfficeRatingWorkingHours(sessionsOld, userId);
                WorkingHours.Dynamic = WorkingHours.TotalAvg - TotalAvgOld;
                WorkingHours.OfficeRatingChanges  = WorkingHours.OfficeRating - OfficeRatingOld;
                jsonToReturn["WorkingHours_SessionsTotal"] = WorkingHours;

                var AvgDialogueTime = new UserWeeklyInfo
                {
                    TotalAvg = userDialogues.Sum(p => p.DialogueHours) / userDialogues.Sum(p => p.Dialogues),
                    AvgPerDay = userDialogues.ToDictionary(x => x.Day, i => (double?)i.DialogueHours / i.Dialogues),
                    OfficeRating = _dbOperation.OfficeRatingDialogueTime(dialogues, userId),
                };
                TotalAvgOld = userDialoguesOld.Sum(p => p.DialogueHours) / userDialoguesOld.Sum(p => p.Dialogues);
                OfficeRatingOld = _dbOperation.OfficeRatingDialogueTime(dialoguesOld, userId);
                AvgDialogueTime.Dynamic = AvgDialogueTime.TotalAvg - TotalAvgOld;
                AvgDialogueTime.OfficeRatingChanges  = AvgDialogueTime.OfficeRating - OfficeRatingOld;
                jsonToReturn["AvgDialogueTime"] = AvgDialogueTime;

                var TotalDialogueTime = new UserWeeklyInfo
                {
                    TotalAvg = userDialogues.Sum(p => p.DialogueHours) ,
                    AvgPerDay = userDialogues.ToDictionary(x => x.Day, i => (double?)i.DialogueHours),
                    OfficeRating = _dbOperation.OfficeRatingDialogueTimeTotal(dialogues, userId),
                };
                TotalAvgOld = userDialoguesOld.Sum(p => p.DialogueHours);
                OfficeRatingOld = _dbOperation.OfficeRatingDialogueTimeTotal(dialoguesOld, userId);
                TotalDialogueTime.Dynamic = TotalDialogueTime.TotalAvg - TotalAvgOld;
                TotalDialogueTime.OfficeRatingChanges  = TotalDialogueTime.OfficeRating - OfficeRatingOld;
                jsonToReturn["TotalDialogueTime"] = TotalDialogueTime;

                var NumberOfDialogues  = new UserWeeklyInfo
                {
                    TotalAvg = userDialogues.Sum(p => p.Dialogues),
                    AvgPerDay = userDialogues.ToDictionary(x => x.Day, i => (double?)i.Dialogues),
                    OfficeRating = _dbOperation.OfficeRatingDialoguesAmount(dialogues, userId),
                };
                TotalAvgOld = userDialoguesOld.Sum(p => p.Dialogues);
                OfficeRatingOld = _dbOperation.OfficeRatingDialoguesAmount(dialoguesOld, userId);
                NumberOfDialogues.Dynamic = NumberOfDialogues.TotalAvg - TotalAvgOld;
                NumberOfDialogues.OfficeRatingChanges  = NumberOfDialogues.OfficeRating - OfficeRatingOld;
                jsonToReturn["NumberOfDialogues"] = NumberOfDialogues; 
                
                var CrossPhrase = new UserWeeklyInfo
                {
                    TotalAvg = userDialogues.Sum(p => (double?)p.CrossDialogues) / userDialogues.Sum(p => (double?)p.Dialogues),
                    AvgPerDay = userDialogues.ToDictionary(x => x.Day, i => (double?)i.CrossDialogues / i.Dialogues),
                    OfficeRating = _dbOperation.OfficeRatingCross(dialogues, userId),
                };
                TotalAvgOld = userDialoguesOld.Sum(p => (double?)p.CrossDialogues) / userDialoguesOld.Sum(p => (double?)p.Dialogues);
                OfficeRatingOld = _dbOperation.OfficeRatingCross(dialoguesOld, userId);
                CrossPhrase.Dynamic = CrossPhrase.TotalAvg - TotalAvgOld;
                CrossPhrase.OfficeRatingChanges  = CrossPhrase.OfficeRating - OfficeRatingOld;
                jsonToReturn["CrossPhrase"] = CrossPhrase;

                var AlertPhrase = new UserWeeklyInfo
                {
                    TotalAvg = userDialogues.Sum(p => (double?)p.AlertDialogues) / userDialogues.Sum(p => (double?)p.Dialogues),
                    AvgPerDay = userDialogues.ToDictionary(x => x.Day, i => (double?)i.AlertDialogues / i.Dialogues),
                    OfficeRating = _dbOperation.OfficeRatingAlert(dialogues, userId),
                };
                TotalAvgOld = userDialoguesOld.Sum(p => (double?)p.AlertDialogues) / userDialoguesOld.Sum(p => (double?)p.Dialogues);
                OfficeRatingOld = _dbOperation.OfficeRatingAlert(dialoguesOld, userId);
                AlertPhrase.Dynamic = AlertPhrase.TotalAvg - TotalAvgOld;
                AlertPhrase.OfficeRatingChanges  = AlertPhrase.OfficeRating - OfficeRatingOld;
                jsonToReturn["AlertPhrase"] = AlertPhrase;

                var LoyaltyPhrase = new UserWeeklyInfo
                {
                    TotalAvg = userDialogues.Sum(p => (double?)p.LoyaltyDialogues) / userDialogues.Sum(p => (double?)p.Dialogues),
                    AvgPerDay = userDialogues.ToDictionary(x => x.Day, i => (double?)i.LoyaltyDialogues / i.Dialogues),
                    OfficeRating = _dbOperation.OfficeRatingLoyalty(dialogues, userId),
                };
                TotalAvgOld = userDialoguesOld.Sum(p => (double?)p.LoyaltyDialogues) / userDialoguesOld.Sum(p => (double?)p.Dialogues);
                OfficeRatingOld = _dbOperation.OfficeRatingLoyalty(dialoguesOld, userId);
                LoyaltyPhrase.Dynamic = LoyaltyPhrase.TotalAvg - TotalAvgOld;
                LoyaltyPhrase.OfficeRatingChanges  = LoyaltyPhrase.OfficeRating - OfficeRatingOld;
                jsonToReturn["LoyaltyPhrase"] = LoyaltyPhrase;

                var NecessaryPhrase = new UserWeeklyInfo
                {
                    TotalAvg = userDialogues.Sum(p => (double?)p.NecessaryDialogues) / userDialogues.Sum(p => (double?)p.Dialogues),
                    AvgPerDay = userDialogues.ToDictionary(x => x.Day, i => (double?)i.NecessaryDialogues / i.Dialogues),
                    OfficeRating = _dbOperation.OfficeRatingNecessary(dialogues, userId),
                };
                TotalAvgOld = userDialoguesOld.Sum(p => (double?)p.NecessaryDialogues) / userDialoguesOld.Sum(p => (double?)p.Dialogues);
                OfficeRatingOld = _dbOperation.OfficeRatingNecessary(dialoguesOld, userId);
                NecessaryPhrase.Dynamic = NecessaryPhrase.TotalAvg - TotalAvgOld;
                NecessaryPhrase.OfficeRatingChanges  = NecessaryPhrase.OfficeRating - OfficeRatingOld;
                jsonToReturn["NecessaryPhrase"] = NecessaryPhrase;

                var FillersPhrase = new UserWeeklyInfo
                {
                    TotalAvg = userDialogues.Sum(p => (double?)p.FillersDialogues) / userDialogues.Sum(p => (double?)p.Dialogues),
                    AvgPerDay = userDialogues.ToDictionary(x => x.Day, i => (double?)i.FillersDialogues / i.Dialogues),
                    OfficeRating = _dbOperation.OfficeRatingFillers(dialogues, userId),
                };
                TotalAvgOld = userDialoguesOld.Sum(p => (double?)p.FillersDialogues) / userDialoguesOld.Sum(p => (double?)p.Dialogues);
                OfficeRatingOld = _dbOperation.OfficeRatingFillers(dialoguesOld, userId);
                FillersPhrase.Dynamic = FillersPhrase.TotalAvg - TotalAvgOld;
                FillersPhrase.OfficeRatingChanges  = FillersPhrase.OfficeRating - OfficeRatingOld;
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