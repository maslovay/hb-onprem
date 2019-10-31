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
using UserOperations.Providers;
//---OLD---
namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticServiceQualityController : Controller
    {
        private readonly AnalyticCommonProvider _analyticProvider;
        private readonly IConfiguration _config;        
        private readonly ILoginService _loginService;
        private readonly DBOperations _dbOperation;
        private readonly RequestFilters _requestFilters;
        private readonly AccountProvider analyticServiceQualityProvider;
        // private readonly ElasticClient _log;

        public AnalyticServiceQualityController(
            AnalyticCommonProvider analyticProvider,
            IConfiguration config,
            ILoginService loginService,
            DBOperations dbOperation,
            RequestFilters requestFilters
            // ElasticClient log
            )
        {
            _analyticProvider = analyticProvider;
            _config = config;
            _loginService = loginService;
            _dbOperation = dbOperation;
            _requestFilters = requestFilters;
            // _log = log;
        }

        [HttpGet("Components")]
        public IActionResult ServiceQualityComponents([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                // _log.Info("AnalyticServiceQuality/Components started");
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);     
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);       

                var phraseTypes = _analyticProvider.GetComponentsPhraseInfo();
                var loyaltyTypeId = phraseTypes.First(p => p.PhraseTypeText == "Loyalty").PhraseTypeId;

                //Dialogues info
                var dialogues = _analyticProvider.GetComponentsDialogueInfo(begTime, endTime, companyIds, applicationUserIds, workerTypeIds,loyaltyTypeId);
                //Result
                var result = new ComponentsSatisfactionInfo
                {
                    EmotionComponent = new ComponentsEmotionInfo {
                        HappinessShare = dialogues.Average(q => q.HappinessShare),
                        NeutralShare = dialogues.Average(q => q.NeutralShare),
                        SurpriseShare = dialogues.Average(q => q.SurpriseShare),
                        SadnessShare = dialogues.Average(q => q.SadnessShare),
                        AngerShare = dialogues.Average(q => q.AngerShare),
                        DisgustShare = dialogues.Average(q => q.DisgustShare),
                        ContemptShare = dialogues.Average(q => q.ContemptShare),
                        FearShare = dialogues.Average(q => q.FearShare),
                    },
                    EmotivityComponent = new ComponentsEmotivityInfo
                    {
                        EmotivityShare = dialogues.Average(q => q.EmotivityShare)
                    },
                    IntonationComponent = new ComponentsIntonationInfo
                    {
                        PositiveTone = dialogues.Average(q => q.PositiveTone),
                        NegativeTone = dialogues.Average(q => q.NegativeTone),
                        NeutralityTone = dialogues.Average(q => q.NeutralityTone),
                    },
                    AttentionComponent = new ComponentsAttentionInfo
                    {
                        AttentionShare = dialogues.Average(q => q.AttentionShare),
                    },
                    PhraseComponent = new ComponentsPhraseTypeInfo
                    {
                        Loyalty = _dbOperation.LoyaltyIndex(dialogues),
                        CrossColour = phraseTypes.FirstOrDefault(q => q.PhraseTypeText == "Cross").Colour,
                        NecessaryColour = phraseTypes.FirstOrDefault(r => r.PhraseTypeText == "Necessary").Colour,
                        LoyaltyColour = phraseTypes.FirstOrDefault(r => r.PhraseTypeText == "Loyalty").Colour,
                        AlertColour = phraseTypes.FirstOrDefault(r => r.PhraseTypeText == "Alert").Colour,
                        FillersColour = phraseTypes.FirstOrDefault(r => r.PhraseTypeText == "Fillers").Colour,
                        RiskColour = phraseTypes.FirstOrDefault(r => r.PhraseTypeText == "Risk").Colour
                    }
                };
                // _log.Info("AnalyticServiceQuality/Components finished");
                return Ok(JsonConvert.SerializeObject(result));
            }
            catch (Exception e )
            {
                // _log.Fatal($"Exception occurred {e}");
                return BadRequest(e);
            }
        }

        [HttpGet("Dashboard")]
        public IActionResult ServiceQualityDashboard([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                // _log.Info("AnalyticServiceQuality/Dashboard started");
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);     
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);       
                var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);

                var dialogues = _analyticProvider.GetDialogues(prevBeg, endTime, companyIds, workerTypeIds, applicationUserIds)
                        .Select(p => new DialogueInfo
                        {
                            DialogueId = p.DialogueId,
                            ApplicationUserId = p.ApplicationUserId,
                            FullName = p.ApplicationUser.FullName,
                            BegTime = p.BegTime,
                            EndTime = p.EndTime,
                            SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                            SatisfactionScoreBeg = p.DialogueClientSatisfaction.FirstOrDefault().BegMoodByNN,
                            SatisfactionScoreEnd = p.DialogueClientSatisfaction.FirstOrDefault().EndMoodByNN
                        })
                        .ToList(); 
                var dialoguesCur = dialogues.Where(p => p.BegTime >= begTime).ToList();
                var dialoguesOld = dialogues.Where(p => p.BegTime < begTime).ToList();


                var result = new ComponentsDashboardInfo
                {
                    SatisfactionIndex = _dbOperation.SatisfactionIndex(dialoguesCur),
                    SatisfactionIndexDelta = - _dbOperation.SatisfactionIndex(dialoguesOld),
                    DialoguesCount = _dbOperation.DialoguesCount(dialoguesCur),
                    DialogueSatisfactionScoreDelta = dialogues.Count() != 0 ? dialoguesCur.Average(p => (p.SatisfactionScoreEnd - p.SatisfactionScoreBeg)): null,
                    Recommendation = "",
                    BestEmployee = _dbOperation.BestEmployee(dialoguesCur),
                    BestEmployeeScore = _dbOperation.BestEmployeeSatisfaction(dialoguesCur),
                    BestProgressiveEmployee = _dbOperation.BestProgressiveEmployee(dialogues, begTime),
                    BestProgressiveEmployeeDelta = _dbOperation.BestProgressiveEmployeeDelta(dialogues, begTime)
                };
                result.SatisfactionIndexDelta += result.SatisfactionIndex;
                // _log.Info("AnalyticServiceQuality/Dashboard finished");
                return Ok(JsonConvert.SerializeObject(result));
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred {e}");
                return BadRequest(e);
            }
        }

        [HttpGet("Rating")]
        public IActionResult ServiceQualityRating([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                // _log.Info("AnalyticServiceQuality/Rating started");
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);     
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
              //  var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);       

                var phrasesTypes = _analyticProvider.GetPhraseTypes();
                //var typeIdCross = phrasesTypes.Where(p => p.PhraseTypeText == "Cross").Select(p => p.PhraseTypeId).First();
                //var typeIdAlert = phrasesTypes.Where(p => p.PhraseTypeText == "Alert").Select(p => p.PhraseTypeId).First();
                //var typeIdNecessary = phrasesTypes.Where(p => p.PhraseTypeText == "Necessary").Select(p => p.PhraseTypeId).First();
                var typeIdLoyalty = phrasesTypes.Where(p => p.PhraseTypeText == "Loyalty").Select(p => p.PhraseTypeId).First();

                var dialogues = _analyticProvider.GetRatingDialogueInfos(begTime, endTime, companyIds, applicationUserIds, workerTypeIds, typeIdLoyalty);

               // return Ok(dialogues.Select(p => p.DialogueId).Distinct().Count());

                var result = dialogues
                    .GroupBy(p => p.ApplicationUserId)
                    .Select(p => new RatingRatingInfo
                    {
                        FullName = p.First().FullName,
                        SatisfactionIndex = p.Any() ? p.Where(q => q.SatisfactionScore != null).Average(q => q.SatisfactionScore) : null,
                        DialoguesCount = p.Any() ? p.Select(q => q.DialogueId).Distinct().Count(): 0,
                        PositiveEmotionShare = p.Any() ? p.Where(q => q.PositiveEmotion!= null).Average(q => q.PositiveEmotion) : null,
                        AttentionShare = p.Any() ? p.Where(q => q.AttentionShare != null).Average(q => q.AttentionShare) : null,
                        PositiveToneShare =p.Any() ? p.Where(q => q.PositiveTone != null).Average(q => q.PositiveTone) : null,
                   //TODO!!!
                        //TextAlertShare =  _dbOperation.AlertIndex(p),
                        //TextCrossShare =  _dbOperation.CrossIndex(p),
                        //TextNecessaryShare =   _dbOperation.NecessaryIndex(p),
                        TextLoyaltyShare = _dbOperation.LoyaltyIndex(p),
                        TextPositiveShare = p.Any()? p.Where(q => q.TextShare != null).Average(q => q.TextShare) : null
                    }).ToList();
               
                result = result.OrderBy(p => p.SatisfactionIndex).ToList();
                // _log.Info("AnalyticServiceQuality/Rating finished");

                return Ok(JsonConvert.SerializeObject(result));
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred {e}");
                return BadRequest(e);
            }
        }

        [HttpGet("SatisfactionStats")]
        public IActionResult ServiceQualitySatisfactionStats([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                // _log.Info("AnalyticServiceQuality/SatisfactionStats started");
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);     
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);       
              //  var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);

                var dialogues = _analyticProvider.GetDialogueInfos(begTime, endTime, companyIds, applicationUserIds, workerTypeIds);

                var result = new SatisfactionStatsInfo
                {
                    AverageSatisfactionScore = dialogues.Average(p => p.SatisfactionScore),
                    PeriodSatisfaction = dialogues
                        .GroupBy(p => p.BegTime.Date)
                        .Select(p => new SatisfactionStatsDayInfo {
                            Date = Convert.ToDateTime(p.Key).ToString(),
                            SatisfactionScore = p.Average(q => q.SatisfactionScore)
                        }).ToList()
                };
                
                result.PeriodSatisfaction = result.PeriodSatisfaction.OrderBy(p => p.Date).ToList();
                // _log.Info("AnalyticServiceQuality/SatisfactionStats finished");
                return Ok(JsonConvert.SerializeObject(result));
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred {e}");
                return BadRequest(e);
            }
        }

        
    }

    

    public class ComponentsDialogueInfo
    {
        public Guid DialogueId;
        public double? PositiveTone;
        public double? NegativeTone;
        public double? NeutralityTone;
        public double? EmotivityShare;
        public double? HappinessShare;
        public double? NeutralShare;
        public double? SurpriseShare;
        public double? SadnessShare;
        public double? AngerShare;
        public double? DisgustShare;
        public double? ContemptShare;
        public double? FearShare;
        public double? AttentionShare;
    //  public double? Cross;
    //  public double? Necessary;
        public int Loyalty;
    //  public double? Alert;
    //  public double? Fillers;
    //  public double? Risk;
    }

    public class ComponentsPhraseInfo
    {
        public Guid PhraseTypeId;
        public string PhraseTypeText;
        public string Colour;
    }

    public class ComponentsPhraseTypeInfo
    {
        public double? Cross;
        public double? Necessary;
        public double? Loyalty;
        public double? Alert;
        public double? Fillers;
        public double? Risk;
        public string CrossColour;
        public string NecessaryColour;
        public string LoyaltyColour;
        public string AlertColour;
        public string FillersColour;
        public string RiskColour;
    }

    public class ComponentsAttentionInfo
    {
        public double? AttentionShare;
    }

    public class ComponentsIntonationInfo
    {
        public double? NegativeTone;
        public double? NeutralityTone;
        public double? PositiveTone;
    }

    public class ComponentsEmotivityInfo
    {
        public double? EmotivityShare;
    }

    public class ComponentsEmotionInfo
    {
        public double? HappinessShare;
        public double? NeutralShare;
        public double? SurpriseShare;
        public double? SadnessShare;
        public double? AngerShare;
        public double? DisgustShare;
        public double? ContemptShare;
        public double? FearShare;
    }

    public class ComponentsSatisfactionInfo
    {
        public ComponentsEmotionInfo EmotionComponent;
        public ComponentsEmotivityInfo EmotivityComponent;
        public ComponentsIntonationInfo IntonationComponent;
        public ComponentsAttentionInfo AttentionComponent;
        public ComponentsPhraseTypeInfo PhraseComponent;
    }

    class ComponentsDashboardInfo
    {
        public double? SatisfactionIndex;
        public double? SatisfactionIndexDelta;
        public int? DialoguesCount;
        public double? DialogueSatisfactionScoreDelta;
        public string Recommendation;
        public string BestEmployee;
        public double? BestEmployeeScore;
        public string BestProgressiveEmployee;
        public double? BestProgressiveEmployeeDelta;
    }

    public class RatingDialogueInfo
    {
       public Guid DialogueId;
        public string ApplicationUserId;
        public DateTime BegTime;
        public DateTime EndTime;
        public string FullName;
        public int CrossCount;
        public int AlertCount;
        public int NecessaryCount;
        public int LoyaltyCount;
        public double? SatisfactionScore;
        public double? PositiveTone;
        public double? AttentionShare;
        public double? PositiveEmotion;
        public double? TextShare;
    }

    public class RatingRatingInfo
    {
        public string FullName;
        public double? SatisfactionIndex;
        public int DialoguesCount;
        public double? PositiveEmotionShare;
        public double? AttentionShare;
        public double? PositiveToneShare;
        //public double? TextAlertShare;
        //public double? TextCrossShare;
        //public double? TextNecessaryShare;
        public double? TextLoyaltyShare;
        public double? TextPositiveShare;
        public string Recomendation;
    }

    public class SatisfactionStatsDayInfo
    {
        public string Date;
        public double? SatisfactionScore;
    }
    public class SatisfactionStatsInfo
    {
        public double? AverageSatisfactionScore;
        public List<SatisfactionStatsDayInfo> PeriodSatisfaction;
            
    }
}