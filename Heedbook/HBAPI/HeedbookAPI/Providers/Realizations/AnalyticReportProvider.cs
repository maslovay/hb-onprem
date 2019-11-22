using System;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBData.Models;
using HBData.Models.AccountViewModels;
using Microsoft.EntityFrameworkCore;
using UserOperations.AccountModels;
using UserOperations.Services;
using Newtonsoft.Json;
using System.Collections.Generic;
using UserOperations.Models.AnalyticModels;
using HBData.Repository;

namespace UserOperations.Providers
{
    public class AnalyticReportProvider : IAnalyticReportProvider
    {
        private readonly IGenericRepository _repository;
        public AnalyticReportProvider(
            IGenericRepository repository
        )
        {
            _repository = repository;
        }
        public List<SessionInfo> GetSessions(
            List<Guid> companyIds,
            List<Guid> applicationUserIds,
            List<Guid> workerTypeIds
        )
        {
            var sessions = _repository.GetAsQueryable<Session>()
                .Where(p =>
                    p.StatusId == 6
                    && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                    && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)))
                .Select(p => new SessionInfo
                {
                    ApplicationUserId = p.ApplicationUserId,
                    FullName = p.ApplicationUser.FullName
                })
                .ToList().Distinct().ToList();            
            return sessions;
        }
        public List<SessionInfo> GetSessions(
            DateTime begTime,
            DateTime endTime,
            Guid employeeRole,
            List<Guid> companyIds,
            List<Guid> applicationUserIds,
            List<Guid> workerTypeIds
        )
        {
            var sessions = _repository.GetAsQueryable<Session>()
                .Where(p =>
                    p.BegTime >= begTime
                    && p.EndTime <= endTime
                    && p.StatusId == 7
                    && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                    && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId))
                    && (p.ApplicationUser.UserRoles.Any(x => x.RoleId == employeeRole)))
                .Select(p => new SessionInfo
                {
                    ApplicationUserId = p.ApplicationUserId,
                    BegTime = p.BegTime,
                    EndTime = p.EndTime,
                    FullName = p.ApplicationUser.FullName,
                    // WorkerType = p.ApplicationUser.WorkerType.WorkerTypeName,
                })
                .ToList();            
            return sessions;            
        }
        public Guid GetEmployeeRoleId()
        {
            var roleId = _repository.GetAsQueryable<ApplicationRole>().FirstOrDefault(x =>x.Name == "Employee").Id;
            return roleId;
        }
        public List<DialogueInfo> GetDialogues(
            DateTime begTime,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid> applicationUserIds,
            List<Guid> workerTypeIds)
        {
            var dialogues = _repository.GetAsQueryable<Dialogue>()
                .Where(p => p.BegTime >= begTime
                    && p.EndTime <= endTime
                    && p.StatusId == 3
                    && p.InStatistic == true
                    && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                    && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)))
                .Select(p => new DialogueInfo
                {
                    DialogueId = p.DialogueId,
                    ApplicationUserId = p.ApplicationUserId,
                    // WorkerType = p.ApplicationUser.WorkerType.WorkerTypeName,
                    FullName = p.ApplicationUser.FullName,
                    BegTime = p.BegTime,
                    EndTime = p.EndTime,
                    SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal
                })
                .ToList();            
            return dialogues;
        }
        public List<DialogueInfo> GetDialoguesWithWorkerType(
            DateTime begTime,
            DateTime endTime,
            Guid employeeRole,
            List<Guid> companyIds,
            List<Guid> applicationUserIds,
            List<Guid> workerTypeIds)
        {
            var dialogues = _repository.GetAsQueryable<Dialogue>()
                .Where(p => p.BegTime >= begTime
                        && p.EndTime <= endTime
                        && p.StatusId == 3
                        && p.InStatistic == true
                        && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                        && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                        && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId))
                        && (p.ApplicationUser.UserRoles.Any(x => x.RoleId == employeeRole)))
                .Select(p => new DialogueInfo
                {
                    DialogueId = p.DialogueId,
                    ApplicationUserId = p.ApplicationUserId,
                    WorkerType = p.ApplicationUser.WorkerType.WorkerTypeName,
                    FullName = p.ApplicationUser.FullName,
                    BegTime = p.BegTime,
                    EndTime = p.EndTime,
                    SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                    // Date = DbFunctions.TruncateTime(p.BegTime)
                })
                .ToList();
            return dialogues;
        }
        public List<ApplicationUser> GetApplicationUsersToAdd(
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid> applicationUserIds,
            List<Guid> workerTypeIds,
            List<Guid> userIds,
            Dictionary<string, string> userClaims,
            Guid employeeRole
        )
        {
            var users = _repository.GetAsQueryable<ApplicationUser>()
                .Where(p =>
                    p.CreationDate <= endTime
                    && p.StatusId == 3
                    &&(!companyIds.Any() || companyIds.Contains((Guid)p.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.Id))
                    && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.WorkerTypeId))
                    && !userIds.Contains(p.Id)
                    && p.Id != Guid.Parse(userClaims["applicationUserId"])
                    && (p.UserRoles.Any(x => x.RoleId == employeeRole)))
                .ToList();

            return users;
        }
    }
}