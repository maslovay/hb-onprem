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

namespace UserOperations.Providers
{
    public class AnalyticOfficeProvider : IAnalyticOfficeProvider
    {
        private readonly RecordsContext _context;
        private readonly ILoginService _loginService;
        public AnalyticOfficeProvider(
            RecordsContext context,
            ILoginService loginService
        )
        {
            _context = context;
            _loginService = loginService;
        }
        public List<SessionInfo> GetSessionsInfo(
            DateTime prevBeg,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid> applicationUserIds,
            List<Guid> workerTypeIds)
        {
            var sessions = _context.Sessions
                .Include(p => p.ApplicationUser)
                .Where(p => p.BegTime >= prevBeg
                    && p.EndTime <= endTime
                    && p.StatusId == 7
                    && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                    && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)))
                .Select(p => new SessionInfo
                {
                    ApplicationUserId = p.ApplicationUserId,
                    BegTime = p.BegTime,
                    EndTime = p.EndTime
                })
                .ToList();
            return sessions;
        }
        public List<DialogueInfo> GetDialoguesInfo(
            DateTime prevBeg,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid> applicationUserIds,
            List<Guid> workerTypeIds)
        {
            var dialogues = _context.Dialogues
                .Include(p => p.ApplicationUser)
                .Where(p => p.BegTime >= prevBeg
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
                    BegTime = p.BegTime,
                    EndTime = p.EndTime,
                    FullName = p.ApplicationUser.FullName,
                    SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal
                })
                .ToList();
            return dialogues;
        }
    }
}