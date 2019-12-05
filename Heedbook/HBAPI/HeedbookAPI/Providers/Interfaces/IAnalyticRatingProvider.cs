using System;
using System.Collections.Generic;
using UserOperations.Models.Get.AnalyticRatingController;

namespace UserOperations.Providers
{
    public interface IAnalyticRatingProvider
    {
        Guid GetCrossPhraseTypeId();
        List<SessionInfo> GetSessions(
            DateTime begTime,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid> applicationUserIds,
            List<Guid> workerTypeIds);

        List<DialogueInfo> GetDialogues(
            DateTime begTime,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid> applicationUserIds,
            List<Guid> workerTypeIds,
            Guid typeIdCross);
        List<SessionInfoCompany> GetSessionInfoCompanys(
            DateTime begTime,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid> workerTypeIds);
        List<DialogueInfoCompany> GetDialogueInfoCompanys(
            DateTime begTime,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid> workerTypeIds,
            Guid typeIdCross);
    }
}