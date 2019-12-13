using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Models.Get.AnalyticServiceQualityController;
using Newtonsoft.Json;
using UserOperations.Utils;
using System.Threading.Tasks;
using UserOperations.Utils.AnalyticServiceQualityUtils;
using UserOperations.Providers;
//---OLD---
namespace UserOperations.Services
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticServiceQualityService : Controller
    {   
        private readonly ILoginService _loginService;
        private readonly IRequestFilters _requestFilters;
        private readonly IAnalyticServiceQualityProvider _analyticServiceQualityProvider;
        private readonly AnalyticServiceQualityUtils _analyticServiceQualityUtils;

        public AnalyticServiceQualityService(
            ILoginService loginService,
            IRequestFilters requestFilters,
            IAnalyticServiceQualityProvider analyticServiceQualityProvider,
            AnalyticServiceQualityUtils analyticServiceQualityUtils
            )
        {
            _loginService = loginService;
            _requestFilters = requestFilters;
            _analyticServiceQualityProvider = analyticServiceQualityProvider;
            _analyticServiceQualityUtils = analyticServiceQualityUtils;
        }

        [HttpGet("Components")]
        public async Task<IActionResult> ServiceQualityComponents([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);     
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);       

                var phraseTypes = await _analyticServiceQualityProvider.GetComponentsPhraseInfo();
                var loyaltyTypeId = phraseTypes.First(p => p.PhraseTypeText == "Loyalty").PhraseTypeId;

                //Dialogues info
                var dialogues = await _analyticServiceQualityProvider.GetComponentsDialogueInfo(begTime, endTime, companyIds, applicationUserIds, workerTypeIds,loyaltyTypeId);
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
                        Loyalty = _analyticServiceQualityUtils.LoyaltyIndex(dialogues),
                        CrossColour = phraseTypes.FirstOrDefault(q => q.PhraseTypeText == "Cross").Colour,
                        NecessaryColour = phraseTypes.FirstOrDefault(r => r.PhraseTypeText == "Necessary").Colour,
                        LoyaltyColour = phraseTypes.FirstOrDefault(r => r.PhraseTypeText == "Loyalty").Colour,
                        AlertColour = phraseTypes.FirstOrDefault(r => r.PhraseTypeText == "Alert").Colour,
                        FillersColour = phraseTypes.FirstOrDefault(r => r.PhraseTypeText == "Fillers").Colour,
                        RiskColour = phraseTypes.FirstOrDefault(r => r.PhraseTypeText == "Risk").Colour
                    }
                };
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
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);     
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);       
                var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);

                var dialogues = _analyticServiceQualityProvider.GetDialoguesIncludedPhrase(prevBeg, endTime, companyIds, workerTypeIds, applicationUserIds)
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
                    SatisfactionIndex = _analyticServiceQualityUtils.SatisfactionIndex(dialoguesCur),
                    SatisfactionIndexDelta = - _analyticServiceQualityUtils.SatisfactionIndex(dialoguesOld),
                    DialoguesCount = _analyticServiceQualityUtils.DialoguesCount(dialoguesCur),
                    DialogueSatisfactionScoreDelta = dialogues.Count() != 0 ? dialoguesCur.Average(p => (p.SatisfactionScoreEnd - p.SatisfactionScoreBeg)): null,
                    Recommendation = "",
                    BestEmployee = _analyticServiceQualityUtils.BestEmployee(dialoguesCur),
                    BestEmployeeScore = _analyticServiceQualityUtils.BestEmployeeSatisfaction(dialoguesCur),
                    BestProgressiveEmployee = _analyticServiceQualityUtils.BestProgressiveEmployee(dialogues, begTime),
                    BestProgressiveEmployeeDelta = _analyticServiceQualityUtils.BestProgressiveEmployeeDelta(dialogues, begTime)
                };
                result.SatisfactionIndexDelta += result.SatisfactionIndex;
                return Ok(JsonConvert.SerializeObject(result));
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred {e}");
                return BadRequest(e);
            }
        }

        [HttpGet("Rating")]
        public async Task<IActionResult> ServiceQualityRating([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);     
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
              //  var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);       

                var phrasesTypes = _analyticServiceQualityProvider.GetPhraseTypes();
                //var typeIdCross = phrasesTypes.Where(p => p.PhraseTypeText == "Cross").Select(p => p.PhraseTypeId).First();
                //var typeIdAlert = phrasesTypes.Where(p => p.PhraseTypeText == "Alert").Select(p => p.PhraseTypeId).First();
                //var typeIdNecessary = phrasesTypes.Where(p => p.PhraseTypeText == "Necessary").Select(p => p.PhraseTypeId).First();
                var typeIdLoyalty = phrasesTypes.Where(p => p.PhraseTypeText == "Loyalty").Select(p => p.PhraseTypeId).First();

                var dialogues = _analyticServiceQualityProvider.GetRatingDialogueInfos(
                    begTime, 
                    endTime, 
                    companyIds, 
                    applicationUserIds, 
                    workerTypeIds, 
                    typeIdLoyalty);

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
                        TextLoyaltyShare = _analyticServiceQualityUtils.LoyaltyIndex(p),
                        TextPositiveShare = p.Any()? p.Where(q => q.TextShare != null).Average(q => q.TextShare) : null
                    }).ToList();
               
                result = result.OrderBy(p => p.SatisfactionIndex).ToList();
                // _log.Info("AnalyticServiceQuality/Rating finished");

                return Ok(JsonConvert.SerializeObject(result));
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpGet("SatisfactionStats")]
        public async Task<IActionResult> ServiceQualitySatisfactionStats([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);     
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);       
              //  var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);

                var dialogues = await _analyticServiceQualityProvider.GetDialogueInfos(begTime, endTime, companyIds, applicationUserIds, workerTypeIds);

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
}