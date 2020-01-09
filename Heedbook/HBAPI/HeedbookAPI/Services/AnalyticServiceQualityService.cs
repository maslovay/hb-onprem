using System;
using System.Collections.Generic;
using System.Linq;
using UserOperations.Models.Get.AnalyticServiceQualityController;
using Newtonsoft.Json;
using UserOperations.Utils;
using System.Threading.Tasks;
using UserOperations.Utils.AnalyticServiceQualityUtils;
using HBData.Repository;
using HBData.Models;
using Microsoft.EntityFrameworkCore;
using UserOperations.Models.AnalyticModels;

namespace UserOperations.Services
{
    public class AnalyticServiceQualityService
    {   
        private readonly LoginService _loginService;
        private readonly RequestFilters _requestFilters;
        private readonly IGenericRepository _repository;
        private readonly AnalyticServiceQualityUtils _analyticServiceQualityUtils;

        public AnalyticServiceQualityService(
            LoginService loginService,
            RequestFilters requestFilters,
            IGenericRepository repository,
            AnalyticServiceQualityUtils analyticServiceQualityUtils
            )
        {
            _loginService = loginService;
            _requestFilters = requestFilters;
            _repository = repository;
            _analyticServiceQualityUtils = analyticServiceQualityUtils;
        }

        public async Task<string> ServiceQualityComponents( string beg, string end, 
                                                        List<Guid?> applicationUserIds, List<Guid> companyIds,
                                                        List<Guid> corporationIds, List<Guid> deviceIds)
        {
                var role = _loginService.GetCurrentRoleName();
                var companyId = _loginService.GetCurrentCompanyId();
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);

                var phraseTypes = await GetComponentsPhraseInfo();
                var loyaltyTypeId = phraseTypes.First(p => p.PhraseTypeText == "Loyalty").PhraseTypeId;

                //Dialogues info
                var dialogues = await GetComponentsDialogueInfo(begTime, endTime, companyIds, applicationUserIds, deviceIds, loyaltyTypeId);
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
                return JsonConvert.SerializeObject(result);
        }

        public string ServiceQualityDashboard( string beg, string end, 
                                                     List<Guid?> applicationUserIds, List<Guid> companyIds, List<Guid> corporationIds,
                                                     List<Guid> deviceIds)
        {
                var role = _loginService.GetCurrentRoleName();
                var companyId = _loginService.GetCurrentCompanyId();
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);
                var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);

                var dialogues = GetDialoguesIncludedPhrase(prevBeg, endTime, companyIds, applicationUserIds, deviceIds)
                        .Select(p => new DialogueInfo
                        {
                            DialogueId = p.DialogueId,
                            ApplicationUserId = p.ApplicationUserId,
                            DeviceId = p.DeviceId,
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
                return JsonConvert.SerializeObject(result);
        }

        public async Task<string> ServiceQualityRating( string beg, string end, 
                                                         List<Guid?> applicationUserIds, List<Guid> companyIds, List<Guid> corporationIds,
                                                         List<Guid> deviceIds )
        {
                var role = _loginService.GetCurrentRoleName();
                var companyId = _loginService.GetCurrentCompanyId();
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);

                var phrasesTypes = GetPhraseTypes();
                //var typeIdCross = phrasesTypes.Where(p => p.PhraseTypeText == "Cross").Select(p => p.PhraseTypeId).First();
                //var typeIdAlert = phrasesTypes.Where(p => p.PhraseTypeText == "Alert").Select(p => p.PhraseTypeId).First();
                //var typeIdNecessary = phrasesTypes.Where(p => p.PhraseTypeText == "Necessary").Select(p => p.PhraseTypeId).First();
                var typeIdLoyalty = phrasesTypes.Where(p => p.PhraseTypeText == "Loyalty").Select(p => p.PhraseTypeId).First();

                var dialogues = GetRatingDialogueInfos(
                    begTime, 
                    endTime, 
                    companyIds, 
                    applicationUserIds,
                    deviceIds, 
                    typeIdLoyalty);

                var result = await dialogues
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
                    }).ToListAsync();
               
                result = result.OrderBy(p => p.SatisfactionIndex).ToList();
                return JsonConvert.SerializeObject(result);
        }

        public async Task<string> ServiceQualitySatisfactionStats( string beg, string end,
                                                    List<Guid?> applicationUserIds, List<Guid> companyIds, List<Guid> corporationIds,
                                                    List<Guid> deviceIds )
        {
                var role = _loginService.GetCurrentRoleName();
                var companyId = _loginService.GetCurrentCompanyId();
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);

                var dialogues = await GetDialogueInfos(begTime, endTime, companyIds, applicationUserIds, deviceIds);

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
                return JsonConvert.SerializeObject(result);
        }

        //---PRIVATE---
        private async Task<List<ComponentsPhraseInfo>> GetComponentsPhraseInfo()
        {
            return await _repository.GetAsQueryable<PhraseType>()
                .Select(p => new ComponentsPhraseInfo
                {
                    PhraseTypeId = p.PhraseTypeId,
                    PhraseTypeText = p.PhraseTypeText,
                    Colour = p.Colour
                }).ToListAsyncSafe();
        }
        private async Task<List<ComponentsDialogueInfo>> GetComponentsDialogueInfo(
            DateTime begTime,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid?> applicationUserIds,
            List<Guid> deviceIds,
            Guid loyaltyTypeId)
        {
            var dialogues = await _repository.GetAsQueryable<Dialogue>()
                .Where(p => p.BegTime >= begTime
                    && p.EndTime <= endTime
                    && p.StatusId == 3
                    && p.InStatistic == true
                    && (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                    && (!deviceIds.Any() || deviceIds.Contains(p.DeviceId)))
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
                .ToListAsyncSafe();
            return dialogues;
        }
        private IQueryable<Dialogue> GetDialoguesIncludedPhrase(
            DateTime begTime,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid?> applicationUserIds = null,
            List<Guid> deviceIds = null)
        {
            var dialogues = _repository.GetAsQueryable<Dialogue>()
                .Where(p => p.BegTime >= begTime
                    && p.EndTime <= endTime
                    && p.StatusId == 3
                    && p.InStatistic == true
                    && (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId))
                    && (applicationUserIds == null || (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId)))
                    && (deviceIds == null || (!deviceIds.Any() || deviceIds.Contains(p.DeviceId))))
                .AsQueryable();
            return dialogues;
        }
        private IQueryable<PhraseType> GetPhraseTypes()
        {
            return _repository.GetAsQueryable<PhraseType>().AsQueryable();
        }
        // public async Task<List<RatingDialogueInfo>> GetRatingDialogueInfos(
        private IQueryable<RatingDialogueInfo> GetRatingDialogueInfos(
        DateTime begTime,
        DateTime endTime,
        List<Guid> companyIds,
        List<Guid?> applicationUserIds,
        List<Guid> deviceIds,
        Guid typeIdLoyalty)
        {
            return _repository.GetAsQueryable<Dialogue>()
                .Where(p => p.BegTime >= begTime
                    && p.EndTime <= endTime
                    && p.StatusId == 3
                    && p.InStatistic == true
                    && (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                    && (!deviceIds.Any() || deviceIds.Contains(p.DeviceId)))
                .Select(p => new RatingDialogueInfo
                {
                    DialogueId = p.DialogueId,
                    ApplicationUserId = p.ApplicationUserId,
                    DeviceId = p.DeviceId,
                    FullName = p.ApplicationUser.FullName,
                    BegTime = p.BegTime,
                    EndTime = p.EndTime,
                    //CrossCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdCross).Count(),
                    //AlertCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdAlert).Count(),
                    //NecessaryCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdNecessary).Count(),
                    LoyaltyCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdLoyalty).Count(),
                    SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                    PositiveTone = p.DialogueAudio.FirstOrDefault().PositiveTone,
                    AttentionShare = p.DialogueVisual.Average(q => q.AttentionShare),
                    PositiveEmotion = p.DialogueVisual.FirstOrDefault().SurpriseShare + p.DialogueVisual.FirstOrDefault().HappinessShare,
                    TextShare = p.DialogueSpeech.FirstOrDefault().PositiveShare,
                })
                .AsQueryable();
            // .ToListAsyncSafe(); 
        }
        private async Task<List<DialogueInfo>> GetDialogueInfos(
            DateTime begTime,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid?> applicationUserIds,
            List<Guid> deviceIds)
        {
            return await _repository.GetAsQueryable<Dialogue>()
                .Where(p => p.BegTime >= begTime
                    && p.EndTime <= endTime
                    && p.StatusId == 3
                    && p.InStatistic == true
                    && (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                    && (!deviceIds.Any() || deviceIds.Contains(p.DeviceId)))
                .Select(p => new DialogueInfo
                {
                    DialogueId = p.DialogueId,
                    ApplicationUserId = p.ApplicationUserId,
                    BegTime = p.BegTime,
                    EndTime = p.EndTime,
                    SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal
                })
                .ToListAsyncSafe();
        }
    }
}