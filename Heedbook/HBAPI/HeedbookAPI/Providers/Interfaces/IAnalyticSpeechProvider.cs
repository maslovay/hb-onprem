using System;
using System.Collections.Generic;
using HBData.Models;
using UserOperations.Models.Get.AnalyticSpeechController;

namespace UserOperations.Providers
{
    public interface IAnalyticSpeechProvider
    {
        Guid GetCrossTypeId();
        Guid GetAlertTypeId();
        List<DialogueInfo> GetDialogueInfos(
            DateTime begTime,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid> applicationUserIds,
            List<Guid> workerTypeIds,
            Guid typeIdCross,
            Guid typeIdAlert);
        List<Guid?> GetCompanyPhrases(List<Guid> companyIds);
        List<Guid> GetDialogueIds(
            DateTime begTime,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid> applicationUserIds,
            List<Guid> workerTypeIds);
        List<PhrasesInfo> GetPhraseInfo(
            List<Guid> dialogueIds,
            List<Guid> phraseIds,
            List<Guid> phraseTypeIds);
        List<PhraseType> GetPhraseTypes();
        List<DialoguePhrasesInfo> DialoguePhrasesInfo(
            List<Guid> dialogueIds,
            List<Guid> phraseIds,
            List<Guid> phraseTypeIds);
        List<DialoguePhrasesInfo> DialoguePhrasesInfo2(
            List<Guid> dialogueIds,
            List<Guid> phraseIds,
            List<Guid> phraseTypeIds);
    }
}