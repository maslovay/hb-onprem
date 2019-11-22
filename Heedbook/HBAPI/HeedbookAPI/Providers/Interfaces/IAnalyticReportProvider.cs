using HBData;
using HBData.Models;
using HBData.Models.AccountViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserOperations.AccountModels;
using UserOperations.Models.AnalyticModels;

namespace UserOperations.Providers
{
    public interface IAnalyticReportProvider
    {
        List<SessionInfo> GetSessions(
            List<Guid> companyIds,
            List<Guid> applicationUserIds,
            List<Guid> workerTypeIds
        );
        Guid GetEmployeeRoleId();
        List<SessionInfo> GetSessions(
            DateTime begTime,
            DateTime endTime,
            Guid employeeRole,
            List<Guid> companyIds,
            List<Guid> applicationUserIds,
            List<Guid> workerTypeIds);
        List<DialogueInfo> GetDialogues(
            DateTime begTime,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid> applicationUserIds,
            List<Guid> workerTypeIds);
        List<ApplicationUser> GetApplicationUsersToAdd(
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid> applicationUserIds,
            List<Guid> workerTypeIds,
            List<Guid> userIds,
            Dictionary<string, string> userClaims,
            Guid employeeRole);
        List<DialogueInfo> GetDialoguesWithWorkerType(
            DateTime begTime,
            DateTime endTime,
            Guid employeeRole,
            List<Guid> companyIds,
            List<Guid> applicationUserIds,
            List<Guid> workerTypeIds);
        
    }
}