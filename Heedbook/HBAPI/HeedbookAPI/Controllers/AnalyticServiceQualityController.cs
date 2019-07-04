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
//---OLD---
namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticServiceQualityController : Controller
    {
        private readonly IConfiguration _config;        
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
        private readonly DBOperations _dbOperation;
        private readonly RequestFilters _requestFilters;
        // private readonly ElasticClient _log;

        public AnalyticServiceQualityController(
            IConfiguration config,
            ILoginService loginService,
            RecordsContext context,
            DBOperations dbOperation,
            RequestFilters requestFilters
            // ElasticClient log
            )
        {
            _config = config;
            _loginService = loginService;
            _context = context;
            _dbOperation = dbOperation;
            _requestFilters = requestFilters;
            // _log = log;
        }

        [HttpGet("Components")]
        public IActionResult ServiceQualityComponents([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationIds[]")] List<Guid> corporationIds,
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
                _requestFilters.CheckRoles(ref companyIds, corporationIds, role, companyId);       

                var phraseTypes = _context.PhraseTypes
                    .Select(p => new ComponentsPhraseInfo {
                        PhraseTypeId = p.PhraseTypeId,
                        PhraseTypeText = p.PhraseTypeText,
                        Colour = p.Colour
                    }).ToList();
                var loyaltyTypeId = phraseTypes.First(p => p.PhraseTypeText == "Loyalty").PhraseTypeId;

                //Dialogues info
                var dialogues = _context.Dialogues
                    .Include(p => p.ApplicationUser)
                    .Include(p => p.DialoguePhraseCount)
                    .Include(p => p.DialogueAudio)
                    .Include(p => p.DialogueSpeech)
                    .Include(p => p.DialogueVisual)
                    .Where(p => p.BegTime >= begTime
                            && p.EndTime <= endTime
                            && p.StatusId == 3
                            && p.InStatistic == true
                            && (!companyIds.Any() || companyIds.Contains((Guid) p.ApplicationUser.CompanyId))
                            && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                            && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid) p.ApplicationUser.WorkerTypeId)))
                    .Select(p => new ComponentsDialogueInfo
                    {
                        DialogueId = p.DialogueId,
                        PositiveTone = p.DialogueAudio.Average(q => q.PositiveTone),
                        NegativeTone = p.DialogueAudio.Average(q => q.NegativeTone),
                        NeutralityTone = p.DialogueAudio.Average(q => q.NeutralityTone),

                        EmotivityShare = p.DialogueSpeech.Average(q => q.PositiveShare),

                        HappinessShare = p.DialogueVisual.Average(q => q.HappinessShare),
                        NeutralShare = p.DialogueVisual.Average(q => q.NeutralShare),
                        SurpriseShare = p.DialogueVisual.Average(q => q.SurpriseShare),
                        SadnessShare = p.DialogueVisual.Average(q => q.SadnessShare),
                        AngerShare = p.DialogueVisual.Average(q => q.AngerShare),
                        DisgustShare = p.DialogueVisual.Average(q => q.DisgustShare),
                        ContemptShare = p.DialogueVisual.Average(q => q.ContemptShare),
                        FearShare = p.DialogueVisual.Average(q => q.FearShare),

                        AttentionShare = p.DialogueVisual.Average(q => q.AttentionShare),
                        Loyalty = p.DialoguePhraseCount.Where(q => q.PhraseTypeId == loyaltyTypeId).Sum(q => q.PhraseCount),
                    })
                    .ToList();
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
                                                        [FromQuery(Name = "corporationIds[]")] List<Guid> corporationIds,
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
                _requestFilters.CheckRoles(ref companyIds, corporationIds, role, companyId);       
                var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);

                var dialogues = _context.Dialogues
                        .Include(p => p.ApplicationUser)
                        .Include(p => p.DialogueClientSatisfaction)
                        .Where(p => p.BegTime >= prevBeg
                                && p.EndTime <= endTime
                                && p.StatusId == 3
                                && p.InStatistic == true
                                && (!companyIds.Any() || companyIds.Contains((Guid) p.ApplicationUser.CompanyId))
                                && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                                && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid) p.ApplicationUser.WorkerTypeId)))
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
                                                        [FromQuery(Name = "corporationIds[]")] List<Guid> corporationIds,
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
                _requestFilters.CheckRoles(ref companyIds, corporationIds, role, companyId);       

                var phrasesTypes = _context.PhraseTypes.ToList();
                var typeIdCross = phrasesTypes.Where(p => p.PhraseTypeText == "Cross").Select(p => p.PhraseTypeId).First();
                var typeIdAlert = phrasesTypes.Where(p => p.PhraseTypeText == "Alert").Select(p => p.PhraseTypeId).First();
                var typeIdNecessary = phrasesTypes.Where(p => p.PhraseTypeText == "Necessary").Select(p => p.PhraseTypeId).First();             

                var dialogues = _context.Dialogues
                        .Include(p => p.ApplicationUser)
                        .Include(p => p.DialogueClientSatisfaction)
                        .Include(p => p.DialoguePhrase)
                        .Include(p => p.DialogueAudio)
                        .Include(p => p.DialogueVisual)
                        .Include(p => p.DialogueSpeech)
                        .Where(p => p.BegTime >= begTime
                                && p.EndTime <= endTime
                                && p.StatusId == 3
                                && p.InStatistic == true
                                && (!companyIds.Any() || companyIds.Contains((Guid) p.ApplicationUser.CompanyId))
                                && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                                && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid) p.ApplicationUser.WorkerTypeId)))
                        .Select(p => new RatingDialogueInfo
                        {
                            DialogueId = p.DialogueId,
                            ApplicationUserId = p.ApplicationUserId.ToString(),
                            FullName = p.ApplicationUser.FullName,
                            BegTime = p.BegTime,
                            EndTime = p.EndTime,
                            CrossCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdCross).Count(),
                            AlertCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdAlert).Count(),
                            NecessaryCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdNecessary).Count(),
                            SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                            PositiveTone = p.DialogueAudio.FirstOrDefault().PositiveTone,
                            AttentionShare = p.DialogueVisual.Average(q => q.AttentionShare),
                            PositiveEmotion = p.DialogueVisual.FirstOrDefault().SurpriseShare + p.DialogueVisual.FirstOrDefault().HappinessShare,
                            TextShare = p.DialogueSpeech.FirstOrDefault().PositiveShare,
                        })
                        .ToList(); 
               // return Ok(dialogues.Select(p => p.DialogueId).Distinct().Count());

                var result = dialogues
                    .GroupBy(p => p.ApplicationUserId)
                    .Select(p => new RatingRatingInfo
                    {
                        FullName = p.First().FullName,
                        SatisfactionIndex = p.Any() ? p.Where(q => q.SatisfactionScore != null && q.SatisfactionScore != 0).Average(q => q.SatisfactionScore) : null,
                        DialoguesCount = p.Any() ? p.Select(q => q.DialogueId).Distinct().Count(): 0,
                        PositiveEmotionShare = p.Any() ? p.Where(q => q.PositiveEmotion!= null && q.PositiveEmotion != 0).Average(q => q.PositiveEmotion) : null,
                        AttentionShare = p.Any() ? p.Where(q => q.AttentionShare != null && q.AttentionShare != 0).Average(q => q.AttentionShare) : null,
                        PositiveToneShare =p.Any() ? p.Where(q => q.PositiveTone != null && q.PositiveTone != 0).Average(q => q.PositiveTone) : null,
                   //TODO!!!
                        TextAlertShare =  _dbOperation.AlertIndex(p),
                        TextCrossShare =  _dbOperation.CrossIndex(p),
                        TextNecessaryShare =   _dbOperation.NecessaryIndex(p),
                        TextPositiveShare = p.Any()? p.Where(q => q.TextShare != null && q.TextShare!= 0).Average(q => q.TextShare) : null
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
                                                        [FromQuery(Name = "corporationIds[]")] List<Guid> corporationIds,
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
                _requestFilters.CheckRoles(ref companyIds, corporationIds, role, companyId);       
              //  var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);

                var dialogues = _context.Dialogues
                    .Include(p => p.ApplicationUser)
                    .Include(p => p.DialogueClientSatisfaction)
                    .Where(p => p.BegTime >= begTime
                            && p.EndTime <= endTime
                            && p.StatusId == 3
                            && p.InStatistic == true
                            && (!companyIds.Any() || companyIds.Contains((Guid) p.ApplicationUser.CompanyId))
                            && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                            && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid) p.ApplicationUser.WorkerTypeId)))
                    .Select(p => new DialogueInfo
                    {
                        DialogueId = p.DialogueId,
                        BegTime = p.BegTime,
                        SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal
                    })
                    .ToList();

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
        public double? TextAlertShare;
        public double? TextCrossShare;
        public double? TextNecessaryShare;
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