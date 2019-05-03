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

        public AnalyticServiceQualityController(
            IConfiguration config,
            ILoginService loginService,
            RecordsContext context,
            DBOperations dbOperation
            )
        {
            _config = config;
            _loginService = loginService;
            _context = context;
            _dbOperation = dbOperation;
        }

        [HttpGet("Components")]
        public IActionResult ServiceQualityComponents([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId")] List<Guid> companyIds,
                                                        [FromQuery(Name = "workerTypeId")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                companyIds = !companyIds.Any()? new List<Guid> { Guid.Parse(userClaims["companyId"])} : companyIds;

                var stringFormat = "yyyyMMdd";
                var begTime = !String.IsNullOrEmpty(beg) ? DateTime.ParseExact(beg, stringFormat, CultureInfo.InvariantCulture) : DateTime.Now.AddDays(-6);
                var endTime = !String.IsNullOrEmpty(end) ? DateTime.ParseExact(end, stringFormat, CultureInfo.InvariantCulture) : DateTime.Now;
                begTime = begTime.Date;
                endTime = endTime.Date.AddDays(1);

                var phraseTypes = _context.PhraseTypes
                    .Select(p => new ComponentsPhraseInfo {
                        PhraseTypeId = p.PhraseTypeId,
                        PhraseTypeText = p.PhraseTypeText,
                        Colour = p.Colour
                    }).ToList();

                var crossTypeId = phraseTypes.First(p => p.PhraseTypeText == "Cross").PhraseTypeId;
                var alertTypeId = phraseTypes.First(p => p.PhraseTypeText == "Alert").PhraseTypeId;
                var fillersTypeId = phraseTypes.First(p => p.PhraseTypeText == "Fillers").PhraseTypeId;
                var loyaltyTypeId = phraseTypes.First(p => p.PhraseTypeText == "Loyalty").PhraseTypeId;
                var neccesaryTypeId = phraseTypes.First(p => p.PhraseTypeText == "Necessary").PhraseTypeId;

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
                        Cross = p.DialoguePhraseCount.Where(q => q.PhraseTypeId == crossTypeId).Count() != 0?
                            p.DialoguePhraseCount.Where(q => q.PhraseTypeId == crossTypeId).Average(q => q.PhraseCount) : 0,
                        Necessary = p.DialoguePhraseCount.Where(q => q.PhraseTypeId == neccesaryTypeId).Count() != 0? 
                            p.DialoguePhraseCount.Where(q => q.PhraseTypeId == neccesaryTypeId).Average(q => q.PhraseCount) : 0,
                        Loyalty = p.DialoguePhraseCount.Where(q => q.PhraseTypeId == loyaltyTypeId).Count() != 0? 
                            p.DialoguePhraseCount.Where(q => q.PhraseTypeId == loyaltyTypeId).Average(q => q.PhraseCount) : 0,
                        Alert = p.DialoguePhraseCount.Where(q => q.PhraseTypeId == alertTypeId).Count() != 0? 
                            p.DialoguePhraseCount.Where(q => q.PhraseTypeId == alertTypeId).Average(q => q.PhraseCount) : 0,
                        Fillers = p.DialoguePhraseCount.Where(q => q.PhraseTypeId == fillersTypeId).Count() != 0? 
                                p.DialoguePhraseCount.Where(q => q.PhraseTypeId == fillersTypeId).Average(q => q.PhraseCount): 0
                    })
                    .ToList();

                ComponentsPhraseTypeInfo Normalization(ComponentsPhraseTypeInfo info)
                {
                    var normCoeff = Convert.ToDouble(info.Cross) + Convert.ToDouble(info.Necessary) + Convert.ToDouble(info.Loyalty) + Convert.ToDouble(info.Alert) + Convert.ToDouble(info.Fillers);
                    info.Cross = normCoeff != 0 ? 100 * info.Cross / normCoeff : null;
                    info.Necessary = normCoeff != 0 ? 100 * info.Necessary / normCoeff : null;
                    info.Loyalty = normCoeff != 0 ? 100 * info.Loyalty / normCoeff : null;
                    info.Alert = normCoeff != 0 ? 100 * info.Alert / normCoeff : null;
                    info.Fillers = normCoeff != 0 ? 100 * info.Fillers / normCoeff : null;
                    return info;
                }
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
                        Cross = dialogues.Average(p => p.Cross),
                        Necessary = dialogues.Average(p => p.Necessary),
                        Loyalty = dialogues.Average(p => p.Loyalty),
                        Alert = dialogues.Average(p => p.Alert),
                        Fillers = dialogues.Average(p => p.Fillers),

                        CrossColour = phraseTypes.FirstOrDefault(q => q.PhraseTypeText == "Cross").Colour,
                        NecessaryColour = phraseTypes.FirstOrDefault(r => r.PhraseTypeText == "Necessary").Colour,
                        LoyaltyColour = phraseTypes.FirstOrDefault(r => r.PhraseTypeText == "Loyalty").Colour,
                        AlertColour = phraseTypes.FirstOrDefault(r => r.PhraseTypeText == "Alert").Colour,
                        FillersColour = phraseTypes.FirstOrDefault(r => r.PhraseTypeText == "Fillers").Colour
                    }
                };

                result.PhraseComponent = Normalization(result.PhraseComponent);
                return Ok(JsonConvert.SerializeObject(result));
            }
            catch (Exception e )
            {
                return BadRequest(e);
            }
        }

        [HttpGet("Dashboard")]
        public IActionResult ServiceQualityDashboard([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId")] List<Guid> companyIds,
                                                        [FromQuery(Name = "workerTypeId")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                companyIds = !companyIds.Any()? new List<Guid> { Guid.Parse(userClaims["companyId"])} : companyIds;

                var stringFormat = "yyyyMMdd";
                var begTime = !String.IsNullOrEmpty(beg) ? DateTime.ParseExact(beg, stringFormat, CultureInfo.InvariantCulture) : DateTime.Now.AddDays(-6);
                var endTime = !String.IsNullOrEmpty(end) ? DateTime.ParseExact(end, stringFormat, CultureInfo.InvariantCulture) : DateTime.Now;
                begTime = begTime.Date;
                endTime = endTime.Date.AddDays(1);
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

                return Ok(JsonConvert.SerializeObject(result));
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpGet("Rating")]
        public IActionResult ServiceQualityRating([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId")] List<Guid> companyIds,
                                                        [FromQuery(Name = "workerTypeId")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                companyIds = !companyIds.Any()? new List<Guid> { Guid.Parse(userClaims["companyId"])} : companyIds;

                var phrasesTypes = _context.PhraseTypes.ToList();
                var typeIdCross = phrasesTypes.Where(p => p.PhraseTypeText == "Cross").Select(p => p.PhraseTypeId).First();
                var typeIdAlert = phrasesTypes.Where(p => p.PhraseTypeText == "Alert").Select(p => p.PhraseTypeId).First();
                var typeIdNecessary = phrasesTypes.Where(p => p.PhraseTypeText == "Necessary").Select(p => p.PhraseTypeId).First();
                
                var stringFormat = "yyyyMMdd";
                var begTime = !String.IsNullOrEmpty(beg) ? DateTime.ParseExact(beg, stringFormat, CultureInfo.InvariantCulture) : DateTime.Now.AddDays(-6);
                var endTime = !String.IsNullOrEmpty(end) ? DateTime.ParseExact(end, stringFormat, CultureInfo.InvariantCulture) : DateTime.Now;
                begTime = begTime.Date;
                endTime = endTime.Date.AddDays(1);
                var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);

                var dialogues = _context.Dialogues
                        .Include(p => p.ApplicationUser)
                        .Include(p => p.DialogueClientSatisfaction)
                        .Include(p => p.DialoguePhraseCount)
                        .Include(p => p.DialogueAudio)
                        .Include(p => p.DialogueVisual)
                        .Include(p => p.DialogueSpeech)
                        .Where(p => p.BegTime >= prevBeg
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
                            CrossCount = p.DialoguePhraseCount.Where(q => q.PhraseTypeId == typeIdCross).Count(),
                            AlertCount = p.DialoguePhraseCount.Where(q => q.PhraseTypeId == typeIdAlert).Count(),
                            NecessaryCount = p.DialoguePhraseCount.Where(q => q.PhraseTypeId == typeIdNecessary).Count(),
                            SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                            PositiveTone = p.DialogueAudio.FirstOrDefault().PositiveTone,
                            AttentionShare = p.DialogueVisual.FirstOrDefault().AttentionShare,
                            PositiveEmotion = p.DialogueVisual.FirstOrDefault().SurpriseShare + p.DialogueVisual.FirstOrDefault().HappinessShare,
                            TextShare = p.DialogueSpeech.FirstOrDefault().PositiveShare,
                        })
                        .ToList(); 

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
                        TextAlertShare = p.Any() ?(double?) p.Average(q => Math.Min(q.AlertCount, 1)): null,
                        TextCrossShare = p.Any() ?(double?) p.Average(q => Math.Min(q.CrossCount, 1)): null,
                        TextNecessaryShare =  p.Any() ?(double?) p.Average(q => Math.Min(q.NecessaryCount, 1)): null,
                        TextPositiveShare = p.Any()? p.Where(q => q.TextShare != null && q.TextShare!= 0).Average(q => q.TextShare) : null
                    }).ToList();
               
                result = result.OrderBy(p => p.SatisfactionIndex).ToList();

                return Ok(JsonConvert.SerializeObject(result));
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpGet("SatisfactionStats")]
        public IActionResult ServiceQualitySatisfactionStats([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId")] List<Guid> companyIds,
                                                        [FromQuery(Name = "workerTypeId")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                companyIds = !companyIds.Any()? new List<Guid> { Guid.Parse(userClaims["companyId"])} : companyIds;

                var stringFormat = "yyyyMMdd";
                var begTime = !String.IsNullOrEmpty(beg) ? DateTime.ParseExact(beg, stringFormat, CultureInfo.InvariantCulture) : DateTime.Now.AddDays(-6);
                var endTime = !String.IsNullOrEmpty(end) ? DateTime.ParseExact(end, stringFormat, CultureInfo.InvariantCulture) : DateTime.Now;
                begTime = begTime.Date;
                endTime = endTime.Date.AddDays(1);
                var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);

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
                return Ok(JsonConvert.SerializeObject(result));
            }
            catch (Exception e)
            {
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
        public double? Cross;
        public double? Necessary;
        public double? Loyalty;
        public double? Alert;
        public double? Fillers;
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
        public string CrossColour;
        public string NecessaryColour;
        public string LoyaltyColour;
        public string AlertColour;
        public string FillersColour;
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