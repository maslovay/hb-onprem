using System;
using System.Collections.Generic;
using System.Linq;
using HBData.Models;
using HBData.Repository;
using Microsoft.EntityFrameworkCore;
using UserOperations.Models.Get.AnalyticRatingController;

namespace UserOperations.Providers
{
    public class AnalyticRatingProvider : IAnalyticRatingProvider
    {
        private readonly IGenericRepository _repository;
        public AnalyticRatingProvider(IGenericRepository repository)
        {
            _repository = repository;
        }
        public Guid GetCrossPhraseTypeId()
        {
            var typeIdCross = _repository.GetAsQueryable<PhraseType>()
                    .Where(p => p.PhraseTypeText == "Cross")
                    .Select(p => p.PhraseTypeId).First();
            return typeIdCross;
        }
        public List<SessionInfo> GetSessions(
            DateTime begTime,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid> applicationUserIds,
            List<Guid> workerTypeIds)
        {
            var sessions = _repository.GetAsQueryable<Session>()
                .Where(p => p.BegTime >= begTime
                    && p.EndTime <= endTime
                    && p.StatusId == 7
                    && (!companyIds.Any() || companyIds.Contains((Guid) p.ApplicationUser.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                    && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid) p.ApplicationUser.WorkerTypeId)))
                .Select(p => new SessionInfo
                {
                    ApplicationUserId = p.ApplicationUserId,
                    BegTime = p.BegTime,
                    EndTime = p.EndTime,
                    FullName = p.ApplicationUser.FullName,
                    CompanyId = p.ApplicationUser.CompanyId                    
                })
                .ToList();
            return sessions;
        }
        public List<DialogueInfo> GetDialogues(
            DateTime begTime,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid> applicationUserIds,
            List<Guid> workerTypeIds,
            Guid typeIdCross)
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
                    CompanyId = p.ApplicationUser.CompanyId,
                    ApplicationUserId = p.ApplicationUserId,
                    BegTime = p.BegTime,
                    EndTime = p.EndTime,
                    SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                    FullName = p.ApplicationUser.FullName,
                    CrossCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdCross).Count()
                })
                .ToList();
            return dialogues;
        }
        public List<SessionInfoCompany> GetSessionInfoCompanys(
            DateTime begTime,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid> workerTypeIds)
        {
            var sessions = _repository.GetAsQueryable<Session>()
                .Where(p => p.BegTime >= begTime
                    && p.EndTime <= endTime
                    && p.StatusId == 7
                    && (!companyIds.Any() || companyIds.Contains((Guid) p.ApplicationUser.CompanyId))
                    && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid) p.ApplicationUser.WorkerTypeId)))
                .Select(p => new SessionInfoCompany
                {
                    CompanyId = (Guid)p.ApplicationUser.CompanyId,
                    BegTime = p.BegTime,
                    EndTime = p.EndTime
                })
                .ToList();
            return sessions;
        }
        public List<DialogueInfoCompany> GetDialogueInfoCompanys(
            DateTime begTime,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid> workerTypeIds,
            Guid typeIdCross)
        {
            var dialogues = _repository.GetAsQueryable<Dialogue>()
                .Where(p => p.BegTime >= begTime
                    && p.EndTime <= endTime
                    && p.StatusId == 3
                    && p.InStatistic == true
                    && (!companyIds.Any() || companyIds.Contains((Guid) p.ApplicationUser.CompanyId))
                    && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid) p.ApplicationUser.WorkerTypeId)))
                .Select(p => new DialogueInfoCompany
                {
                    DialogueId = p.DialogueId,
                    CompanyId = (Guid)p.ApplicationUser.CompanyId,
                    BegTime = p.BegTime,
                    EndTime = p.EndTime,
                    SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                    FullName = p.ApplicationUser.Company.CompanyName,
                    CrossCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdCross).Count()
                })
                .ToList();
            return dialogues;
        }
    }
}