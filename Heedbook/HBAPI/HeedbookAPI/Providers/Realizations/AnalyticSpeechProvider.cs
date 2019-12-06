using System;
using System.Collections.Generic;
using System.Linq;
using HBData.Models;
using HBData.Repository;
using UserOperations.Models.Get.AnalyticSpeechController;

namespace UserOperations.Providers
{
    public class AnalyticSpeechProvider : IAnalyticSpeechProvider
    {
        private readonly IGenericRepository _repository;
        public AnalyticSpeechProvider(
            IGenericRepository repository
        )
        {
            _repository = repository;
        }
        public Guid GetCrossTypeId()
        {
            var typeIdCross = _repository.GetAsQueryable<PhraseType>()
                .Where(p => p.PhraseTypeText == "Cross")
                .Select(p => p.PhraseTypeId).First();
            return typeIdCross;
        }
        public Guid GetAlertTypeId()
        {
            var typeIdAlert = _repository.GetAsQueryable<PhraseType>()
                .Where(p => p.PhraseTypeText == "Alert")
                .Select(p => p.PhraseTypeId).First();
            return typeIdAlert;
        }
        public List<DialogueInfo> GetDialogueInfos(
            DateTime begTime,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid> applicationUserIds,
            List<Guid> workerTypeIds,
            Guid typeIdCross,
            Guid typeIdAlert)
        {
            var dialogues = _repository.GetAsQueryable<Dialogue>()
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
                    ApplicationUserId = p.ApplicationUserId,
                    BegTime = p.BegTime,
                    EndTime = p.EndTime,
                    SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                    FullName = p.ApplicationUser.FullName,
                    CrossCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdCross).Count(),
                    AlertCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdAlert).Count(),
                })
                .ToList();
            return dialogues;
        }
        public List<Guid?> GetCompanyPhrases(List<Guid> companyIds)
        {
            var companysPhrases = _repository.GetAsQueryable<PhraseCompany>()
                .Where(p => (!companyIds.Any() || companyIds.Contains((Guid) p.CompanyId)))
                .Select(p => p.PhraseId)
                .ToList();
            return companysPhrases;
        }
        public List<Guid> GetDialogueIds(
            DateTime begTime,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid> applicationUserIds,
            List<Guid> workerTypeIds)
        {
            var dialogueIds = _repository.GetAsQueryable<Dialogue>()
                .Where(p => p.EndTime >= begTime
                    && p.EndTime <= endTime
                    && p.StatusId == 3
                    && p.InStatistic == true)
                .Where(p => (!companyIds.Any() || companyIds.Contains((Guid) p.ApplicationUser.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                    && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid) p.ApplicationUser.WorkerTypeId)))
                .Select(p => p.DialogueId).ToList();
            return dialogueIds;
        }
        
        public List<PhrasesInfo> GetPhraseInfo(
            List<Guid> dialogueIds,
            List<Guid> phraseIds,
            List<Guid> phraseTypeIds)
        {
            var phrasesInfo = _repository.GetAsQueryable<DialoguePhrase>()
                .Where(p => p.DialogueId.HasValue 
                    && dialogueIds.Contains(p.DialogueId.Value)
                    && (!phraseIds.Any() || phraseIds.Contains((Guid) p.PhraseId))
                    && (!phraseTypeIds.Any() || phraseTypeIds.Contains((Guid) p.Phrase.PhraseTypeId))
                    //&& (companysPhrases.Contains(p.PhraseId))
                    )
                .Select(p => new PhrasesInfo{
                    IsClient = p.IsClient,
                    FullName = p.Dialogue.ApplicationUser.FullName,
                    ApplicationUserId = p.Dialogue.ApplicationUserId,
                    DialogueId = p.DialogueId,
                    PhraseId = p.PhraseId,
                    PhraseText = p.Phrase.PhraseText,
                    PhraseTypeText = p.Phrase.PhraseType.PhraseTypeText
                })
                .ToList();
            return phrasesInfo;
        }
        
        public List<DialoguePhrasesInfo> DialoguePhrasesInfo(
            List<Guid> dialogueIds,
            List<Guid> phraseIds,
            List<Guid> phraseTypeIds
        )
        {
            var requestPhrase = _repository.GetAsQueryable<DialoguePhrase>()
                .Where(p => p.DialogueId.HasValue && dialogueIds.Contains(p.DialogueId.Value)
                    && (!phraseIds.Any() || phraseIds.Contains((Guid) p.PhraseId))
                    && (!phraseTypeIds.Any() || phraseTypeIds.Contains((Guid) p.Phrase.PhraseTypeId)))
                .Select(p => new DialoguePhrasesInfo{
                    IsClient = p.IsClient,
                    PhraseType = p.Phrase.PhraseType.PhraseTypeText,
                    Colour = p.Phrase.PhraseType.Colour,
                    DialogueId = p.DialogueId
                }).ToList();   
            return requestPhrase;
        }
        public List<PhraseType> GetPhraseTypes()
        {
            return _repository.GetAsQueryable<PhraseType>().ToList();
        }
        public List<DialoguePhrasesInfo> DialoguePhrasesInfo2(
            List<Guid> dialogueIds,
            List<Guid> phraseIds,
            List<Guid> phraseTypeIds
        )
        {
            var phrases = _repository.GetAsQueryable<DialoguePhrase>()
                .Where(p => p.DialogueId.HasValue 
                    && dialogueIds.Contains(p.DialogueId.Value)
                    && (!phraseIds.Any() || phraseIds.Contains((Guid) p.PhraseId))
                    && (!phraseTypeIds.Any() || phraseTypeIds.Contains((Guid) p.Phrase.PhraseTypeId)))
                .Select(p =>new DialoguePhrasesInfo{
                    PhraseText = p.Phrase.PhraseText,
                    PhraseColor = p.Phrase.PhraseType.Colour
                }).ToList();
            return phrases;
        }
    }        
}