using System;
using System.Collections.Generic;
using System.Linq;
using HBData.Models;
using Newtonsoft.Json;
using UserOperations.Models.Get.AnalyticSpeechController;
using HBData.Repository;
using UserOperations.Models.AnalyticModels;
using UserOperations.Utils.Interfaces;
using HBLib.Utils.Interfaces;

namespace UserOperations.Services
{
    public class AnalyticSpeechService
    {  
        private readonly ILoginService _loginService;
        private readonly IRequestFilters _requestFilters;
        private readonly IGenericRepository _repository;
        private readonly IAnalyticSpeechUtils _analyticSpeechUtils;


        public AnalyticSpeechService(
            ILoginService loginService,
            IRequestFilters requestFilters,
            IGenericRepository repository,
            IAnalyticSpeechUtils analyticSpeechUtils
            )
        {
            _loginService = loginService;
            _requestFilters = requestFilters;
            _repository = repository;
            _analyticSpeechUtils = analyticSpeechUtils;
        }

        public string SpeechEmployeeRating( string beg, string end, 
                                            List<Guid?> applicationUserIds, List<Guid> companyIds, List<Guid> corporationIds,
                                            List<Guid> deviceIds
                                                        // List<Guid> phraseIds, List<Guid> phraseTypeIds
                                            )
        {
                var role = _loginService.GetCurrentRoleName();
                var companyId = _loginService.GetCurrentCompanyId();

                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId); 
                var typeIdCross = GetCrossTypeId();
                var typeIdAlert = GetAlertTypeId();

                var dialogues = GetDialogueInfos(
                    begTime,
                    endTime,
                    companyIds,
                    applicationUserIds,
                    deviceIds,
                    typeIdCross,
                    typeIdAlert);
              
                var result = dialogues
                    .GroupBy(p => p.ApplicationUserId)
                    .Select(p => new
                    {
                        FullName = p.First().FullName,
                        ApplicationUserId = p.Key,
                        CrossFreq = _analyticSpeechUtils.CrossIndex(p),
                        AlertFreq = _analyticSpeechUtils.AlertIndex(p)
                    });
                return JsonConvert.SerializeObject(result);
        }

        public string SpeechPhraseTable( string beg, string end, 
                                         List<Guid?> applicationUserIds, List<Guid> companyIds, List<Guid> corporationIds,
                                         List<Guid> deviceIds, List<Guid> phraseIds, List<Guid> phraseTypeIds )
        {
                var role = _loginService.GetCurrentRoleName();
                var companyId = _loginService.GetCurrentCompanyId();
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);                  

                // var companysPhrases = _analyticSpeechProvider.GetCompanyPhrases(companyIds);
                
                var dialogueIds = GetDialogueDeviceIds(
                    begTime,
                    endTime,
                    companyIds,
                    applicationUserIds,
                    deviceIds);

                var dialoguesTotal = dialogueIds.Count();

                var companyPhraseIds = _repository.GetAsQueryable<PhraseCompany>()
                    .Where(p => companyIds.Contains((Guid)p.CompanyId))
                    .Select(p => (Guid)p.PhraseId)
                    .Distinct()
                    .ToList();
                
                // GET ALL PHRASES INFORMATION
                var phrasesInfo = GetPhraseInfo(
                    dialogueIds,
                    phraseIds,
                    phraseTypeIds);

                var result = phrasesInfo
                    .Where(p => companyPhraseIds.Contains((Guid)p.PhraseId))
                    .GroupBy(p => p.PhraseText.ToLower())
                    .Select(p => new {
                        Phrase = p.Key,
                        PhraseId = p.First().PhraseId,
                        PopularName = p.GroupBy(q => q.FullName)
                            .OrderByDescending(q => q.Count())
                            .Select(g => g.Key)
                            .First(),
                        PhraseType = p.First().PhraseTypeText,
                        Percent = dialogueIds.Any() ? Math.Round(100 * Convert.ToDouble(p.Select(q => q.DialogueId).Distinct().Count()) / Convert.ToDouble(dialoguesTotal), 1) : 0,
                        Freq = Convert.ToDouble(p.Select(q => q.DialogueId).Distinct().Count()) != 0 ?
                            Math.Round(Convert.ToDouble(p.GroupBy(q => q.ApplicationUserId).Max(q => q.Count())) / Convert.ToDouble(p.Select(q => q.DialogueId).Distinct().Count()), 2) :
                            0
                    });
                return JsonConvert.SerializeObject(result);
        }

        public SpeechPhraseTotalInfo SpeechPhraseTypeCount( string beg, string end, 
                                             List<Guid?> applicationUserIds, List<Guid> companyIds, List<Guid> corporationIds,
                                             List<Guid> deviceIds, List<Guid> phraseIds, List<Guid> phraseTypeIds )
        {
                var role = _loginService.GetCurrentRoleName();
                var companyId = _loginService.GetCurrentCompanyId();
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);

                var dialogueIds = GetDialogueDeviceIds(
                    begTime,
                    endTime,
                    companyIds,
                    applicationUserIds,
                    deviceIds);
                // CREATE PARAMETERS
                var totalInfo = new SpeechPhraseTotalInfo();

                var requestPhrase = DialoguePhrasesInfo(
                    dialogueIds,
                    phraseIds,
                    phraseTypeIds);
                   
                var total = requestPhrase
                    .GroupBy(p => p.PhraseType)
                    .Select(p => new SpeechPhrasesInfo
                    {
                        Type = p.Key,
                        Count = (dialogueIds.Count() != 0) ? Math.Round(100 * Convert.ToDouble(p.Select(q => q.DialogueId).Distinct().Count()) / Convert.ToDouble(dialogueIds.Count())) : 0,
                        Colour = p.First().Colour
                    }).ToList();

                var types = GetPhraseTypes();
                foreach (var type in types)
                {
                    if (total.Where(p => p.Type == type.PhraseTypeText).Count() == 0)
                        total.Add(new SpeechPhrasesInfo
                        {
                            Type = type.PhraseTypeText,
                            Count = 0,
                            Colour = type.Colour
                        });
                }
                totalInfo.Total = total;
                return totalInfo;
        }

        public string SpeechWordCloud( string beg, string end, List<Guid?> applicationUserIds, List<Guid> companyIds,
                                       List<Guid> corporationIds, List<Guid> deviceIds, List<Guid> phraseIds,
                                       List<Guid> phraseTypeIds )
        {
                var role = _loginService.GetCurrentRoleName();
                var companyId = _loginService.GetCurrentCompanyId();
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);       

                var dialogueIds = GetDialogueDeviceIds(
                    begTime,
                    endTime,
                    companyIds,
                    applicationUserIds,
                    deviceIds);

                var phrases = DialoguePhrasesInfoAsQueryable(
                    dialogueIds,
                    phraseIds,
                    phraseTypeIds);

                var result = phrases.GroupBy(p => p.PhraseText)
                    .Select(p => new {
                        Text = p.First().PhraseText,
                        Weight = 2 * p.Count(),
                        Colour = p.First().PhraseColor});
                return JsonConvert.SerializeObject(result);
        }


        public string PhraseSalesStageCount(string beg, string end, 
                                        Guid? corporationId,
                                        List<Guid> companyIds, List<Guid?> applicationUserIds, 
                                        List<Guid> deviceIds, List<Guid> phraseIds,
                                        List<Guid> salesStageIds)
        {
            var role = _loginService.GetCurrentRoleName();
            var companyId = _loginService.GetCurrentCompanyId();
            var begTime = _requestFilters.GetBegDate(beg);
            var endTime = _requestFilters.GetEndDate(end);

            if(corporationId == null && !companyIds.Any())
            {
                var company = _repository.GetAsQueryable<Company>()
                    .FirstOrDefault(p => p.CompanyId == companyId);
                if(company.CorporationId != null)
                    corporationId = company.CorporationId;
            }

            if (corporationId == null && companyIds.Any())
            {
                corporationId = _repository.GetAsQueryable<Company>()
                       .Where(x => x.CompanyId == companyIds.First()).Select(x => x.CorporationId).FirstOrDefault();
            }

            if (corporationId != null)
            {
                var companyInCorporatioIds = _repository.GetAsQueryable<Corporation>()
                       .Where(x => x.Id == corporationId).SelectMany(x => x.Companies.Select(p => p.CompanyId)).ToList();
                companyIds = companyIds.Concat(companyInCorporatioIds).ToList();
                var resultForCorp = SalesStagePhraseForCorporation(
                        begTime, endTime, 
                        corporationId, companyIds, applicationUserIds,
                        deviceIds, phraseIds, salesStageIds);
                return JsonConvert.SerializeObject(resultForCorp);
            }

            var result = SalesStagePhraseForOneCompany(
                        begTime, endTime,
                        applicationUserIds, companyId,
                        deviceIds, phraseIds, salesStageIds);
            return JsonConvert.SerializeObject(result);
        }


        //---PRIVATE---
        private Guid GetCrossTypeId()
        {
            var typeIdCross = _repository.GetAsQueryable<PhraseType>()
                .Where(p => p.PhraseTypeText == "Cross")
                .Select(p => p.PhraseTypeId).First();
            return typeIdCross;
        }
        private Guid GetAlertTypeId()
        {
            var typeIdAlert = _repository.GetAsQueryable<PhraseType>()
                .Where(p => p.PhraseTypeText == "Alert")
                .Select(p => p.PhraseTypeId).First();
            return typeIdAlert;
        }
        private IQueryable<DialogueInfo> GetDialogueInfos(
            DateTime begTime,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid?> applicationUserIds,
            List<Guid> deviceIds,
            Guid typeIdCross,
            Guid typeIdAlert)
        {
            var dialogues = _repository.GetAsQueryable<Dialogue>()
                .Where(p => p.BegTime >= begTime
                    && p.EndTime <= endTime
                    && p.StatusId == 3
                    && p.InStatistic == true
                    && (!companyIds.Any() || companyIds.Contains((Guid)p.Device.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                    && (!deviceIds.Any() || deviceIds.Contains(p.DeviceId))
                    && p.ApplicationUserId != null)
                .Select(p => new DialogueInfo
                {
                    DialogueId = p.DialogueId,
                    ApplicationUserId = p.ApplicationUserId,
                    DeviceId = p.DeviceId,
                    BegTime = p.BegTime,
                    EndTime = p.EndTime,
                    SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                    FullName = p.ApplicationUser.FullName,
                    CrossCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdCross).Count(),
                    AlertCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdAlert).Count(),
                })
                .AsQueryable();
            return dialogues;
        }
        private List<Guid?> GetCompanyPhrases(List<Guid> companyIds)
        {
            var companysPhrases = _repository.GetAsQueryable<PhraseCompany>()
                .Where(p => (!companyIds.Any() || companyIds.Contains((Guid)p.CompanyId)))
                .Select(p => p.PhraseId)
                .ToList();
            return companysPhrases;
        }
        private List<Guid> GetDialogueDeviceIds(
            DateTime begTime,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid?> applicationUserIds,
            List<Guid> deviceIds)
        {
            var dialogueIds = _repository.GetAsQueryable<Dialogue>()
                .Where(p => p.EndTime >= begTime
                    && p.EndTime <= endTime
                    && p.StatusId == 3
                    && p.InStatistic == true)
                .Where(p => (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId))
                    && (!applicationUserIds.Any() || ( p.ApplicationUserId != null && applicationUserIds.Contains(p.ApplicationUserId)))
                    && (!deviceIds.Any() || deviceIds.Contains(p.DeviceId)))
                .Select(p => p.DialogueId).ToList();
            return dialogueIds;
        }

        private List<Guid> GetDialogueUserIds(
          DateTime begTime,
          DateTime endTime,
          List<Guid> companyIds,
          List<Guid?> applicationUserIds,
          List<Guid> deviceIds)
        {
            var dialogueIds = _repository.GetAsQueryable<Dialogue>()
                .Where(p => p.EndTime >= begTime
                    && p.EndTime <= endTime
                    && p.StatusId == 3
                    && p.InStatistic == true
                    && p.ApplicationUserId != null)
                .Where(p => (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                    && (!deviceIds.Any() || deviceIds.Contains(p.DeviceId)))
                .Select(p => p.DialogueId).ToList();
            return dialogueIds;
        }

        private IQueryable<PhrasesInfo> GetPhraseInfo(
            List<Guid> dialogueIds,
            List<Guid> phraseIds,
            List<Guid> phraseTypeIds)
        {
            var phrasesInfo = _repository.GetAsQueryable<DialoguePhrase>()
                .Where(p => p.DialogueId.HasValue
                    && dialogueIds.Contains(p.DialogueId.Value)
                    && (!phraseIds.Any() || phraseIds.Contains((Guid)p.PhraseId))
                    && (!phraseTypeIds.Any() || phraseTypeIds.Contains((Guid)p.Phrase.PhraseTypeId))
                    //&& (companysPhrases.Contains(p.PhraseId))
                    )
                .Select(p => new PhrasesInfo
                {
                    IsClient = p.IsClient,
                    FullName = p.Dialogue.ApplicationUser.FullName,
                    ApplicationUserId = p.Dialogue.ApplicationUserId,
                    DialogueId = p.DialogueId,
                    PhraseId = p.PhraseId,
                    PhraseText = p.Phrase.PhraseText,
                    PhraseTypeText = p.Phrase.PhraseType.PhraseTypeText
                })
                .AsQueryable();
            return phrasesInfo;
        }

        private List<DialoguePhrasesInfo> DialoguePhrasesInfo(
            List<Guid> dialogueIds,
            List<Guid> phraseIds,
            List<Guid> phraseTypeIds
        )
        {
            var requestPhrase = _repository.GetAsQueryable<DialoguePhrase>()
                .Where(p => p.DialogueId.HasValue && dialogueIds.Contains(p.DialogueId.Value)
                    && (!phraseIds.Any() || phraseIds.Contains((Guid)p.PhraseId))
                    && (!phraseTypeIds.Any() || phraseTypeIds.Contains((Guid)p.Phrase.PhraseTypeId)))
                .Select(p => new DialoguePhrasesInfo
                {
                    IsClient = p.IsClient,
                    PhraseType = p.Phrase.PhraseType.PhraseTypeText,
                    Colour = p.Phrase.PhraseType.Colour,
                    DialogueId = p.DialogueId
                }).ToList();
            return requestPhrase;
        }
        private List<PhraseType> GetPhraseTypes()
        {
            return _repository.GetAsQueryable<PhraseType>().ToList();
        }
        private IQueryable<DialoguePhrasesInfo> DialoguePhrasesInfoAsQueryable(
            List<Guid> dialogueIds,
            List<Guid> phraseIds,
            List<Guid> phraseTypeIds
        )
        {
            var phrases = _repository.GetAsQueryable<DialoguePhrase>()
                .Where(p => p.DialogueId.HasValue
                    && dialogueIds.Contains(p.DialogueId.Value)
                    && (!phraseIds.Any() || phraseIds.Contains((Guid)p.PhraseId))
                    && (!phraseTypeIds.Any() || phraseTypeIds.Contains((Guid)p.Phrase.PhraseTypeId)))
                .Select(p => new DialoguePhrasesInfo
                {
                    PhraseText = p.Phrase.PhraseText,
                    PhraseColor = p.Phrase.PhraseType.Colour
                })
                .AsQueryable();
            return phrases;
        }


        private List<SalesStagePhraseModel> SalesStagePhraseForCorporation(
                    DateTime begTime,
                    DateTime endTime, 
                    Guid? corporationId,
                    List<Guid> companyIds,
                    List<Guid?> applicationUserIds, 
                    List<Guid> deviceIds, 
                    List<Guid> phraseIds,
                    List<Guid> salesStageIds
     )
        {
            var dialogueIds = companyIds.Count()==0? new List<Guid>() : GetDialogueDeviceIds(begTime, endTime, companyIds, applicationUserIds, deviceIds);

            var phrases = _repository.GetAsQueryable<DialoguePhrase>()
                    .Where(p => p.PhraseId != null
                    && (!phraseIds.Any() || phraseIds.Contains((Guid)p.PhraseId))
                    && (!dialogueIds.Any() || dialogueIds.Contains((Guid)p.DialogueId)))
                    .ToList();

            var phrasesSalesStages = _repository.GetAsQueryable<SalesStagePhrase>()
                    .Where(x =>
                    (!salesStageIds.Any() || salesStageIds.Contains((Guid)x.SalesStageId))
                    && (x.CorporationId == corporationId))
                    .GroupBy(x => x.SalesStageId)
                    .Select(k => new SalesStagePhraseModel
                    {
                        Count = CountPhrasesAmount(phrases, k, phraseIds),
                        SalesStageId = k.Key,
                        SalesStageName = k.FirstOrDefault().SalesStage.Name,
                        SequenceNumber = k.FirstOrDefault().SalesStage.SequenceNumber
                    }).ToList();

            var dialogueCount = 0;
            foreach (var item in phrasesSalesStages)
            {
                dialogueCount = dialogueIds.Count();
                item.PercentageOfExecution = dialogueCount == 0? 0: (double)item.Count / dialogueCount;
            }
            return phrasesSalesStages;
        }

    private List<SalesStagePhraseModel> SalesStagePhraseForOneCompany(
                DateTime begTime,
                DateTime endTime,
                List<Guid?> applicationUserIds,
                Guid companyId,
                List<Guid> deviceIds,
                List<Guid> phraseIds,
                List<Guid> salesStageIds
 )
    {
            var corporationId = _repository.GetAsQueryable<Company>()
              .Where(x => x.CompanyId == companyId).Select(x => x.CorporationId).FirstOrDefault();
            var dialogueIds = GetDialogueDeviceIds(begTime, endTime, new List<Guid> { companyId }, applicationUserIds, deviceIds);

            var phrases = _repository.GetAsQueryable<DialoguePhrase>()
                    .Where(p => p.PhraseId != null
                    && (!phraseIds.Any() || phraseIds.Contains((Guid)p.PhraseId))
                    && (!dialogueIds.Any() || dialogueIds.Contains((Guid)p.DialogueId)))
                    .ToList();

            var phrasesSalesStages = _repository.GetAsQueryable<SalesStagePhrase>()
                    .Where(x =>
                    (!salesStageIds.Any() || salesStageIds.Contains((Guid)x.SalesStageId))
                    && (x.CompanyId == companyId || (corporationId != null && x.CorporationId == corporationId)))
                    .GroupBy(x => x.SalesStageId)
                    .Select(k => new SalesStagePhraseModel
                    {
                        Count = CountPhrasesAmount( phrases, k, phraseIds),
                        SalesStageId = k.Key,
                        SalesStageName = k.FirstOrDefault().SalesStage.Name,
                        SequenceNumber=  k.FirstOrDefault().SalesStage.SequenceNumber
                    }).ToList();

            var dialogueCount = 0;
            foreach (var item in phrasesSalesStages)
            {
                dialogueCount = dialogueIds.Count();
                item.PercentageOfExecution = dialogueCount == 0 ? 0 : (double)item.Count / dialogueCount;
            }
            return phrasesSalesStages;
        }

        private int CountPhrasesAmount(List<DialoguePhrase> dialoguePhr, IGrouping<Guid, SalesStagePhrase> ssPhr, List<Guid> phraseIds)
        {
            var count = dialoguePhr
                                  .Where(p => ssPhr.Select(s => s.PhraseId)
                                  .Contains((Guid)p.PhraseId)
                                  )
                                  .Select(p => p.DialogueId).Distinct().Count();
            return count;
        }
    }
}
