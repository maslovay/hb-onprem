using HBData;
using HBData.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserOperations.Models.AnalyticModels;

namespace UserOperations.Providers
{
    public class AnalyticCommonProvider
    {
        private readonly RecordsContext _context;
        public AnalyticCommonProvider(RecordsContext context)
        {
            _context = context;
        }
        public async Task<List<SessionInfo>> GetSessionInfoAsync( DateTime begTime, DateTime endTime, List<Guid> companyIds, List<Guid> workerTypeIds, List<Guid> userIds = null)
        {
            var sessions = await _context.Sessions
                         .Include(p => p.ApplicationUser)
                         .Where(p => p.BegTime >= begTime
                                 && p.EndTime <= endTime
                                 && p.StatusId == 7
                                 && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                                 && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId))
                                 && (userIds == null || (!userIds.Any() || userIds.Contains(p.ApplicationUserId))))
                         .Select(p => new SessionInfo
                         {
                             ApplicationUserId = p.ApplicationUserId,
                             BegTime = p.BegTime,
                             EndTime = p.EndTime
                         })
                         .ToListAsync();
            return sessions;
        }

        private IQueryable<Dialogue> GetDialogues(DateTime begTime, DateTime endTime, List<Guid> companyIds = null, List<Guid> applicationUserIds = null, List<Guid> workerTypeIds = null)
        {
            var data = _context.Dialogues
                     .Where(p => p.BegTime >= begTime &&
                         p.EndTime <= endTime &&
                         p.StatusId == 3 &&
                         p.InStatistic == true &&
                         (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId)) &&
                         (!applicationUserIds.Any() || applicationUserIds.Contains((Guid)p.ApplicationUserId)) &&
                         (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId))).AsQueryable();
            return data;
        }

        public IQueryable<Dialogue> GetDialoguesIncludedPhrase(DateTime begTime, DateTime endTime, List<Guid> companyIds, List<Guid> workerTypeIds, List<Guid> applicationUserIds = null)
        {
            var dialogues = _context.Dialogues
                       .Include(p => p.ApplicationUser)
                       .Include(p => p.DialogueClientSatisfaction)
                       .Include(p => p.DialoguePhrase)
                       .Where(p => p.BegTime >= begTime
                               && p.EndTime <= endTime
                               && p.StatusId == 3
                               && p.InStatistic == true
                               && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                               && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId))
                               && (applicationUserIds == null || (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId)))).AsQueryable();
            return dialogues;
        }

        public IQueryable<Dialogue> GetDialoguesIncludedClientProfile(DateTime begTime, DateTime endTime, List<Guid> companyIds, List<Guid> applicationUserIds, List<Guid> workerTypeIds)
        {
            var data = _context.Dialogues
                     .Include(p => p.DialogueClientProfile)
                     .Include(p => p.ApplicationUser)
                     .Where(p => p.BegTime >= begTime &&
                         p.EndTime <= endTime &&
                         p.StatusId == 3 &&
                         p.InStatistic == true &&
                         (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId)) &&
                         (!applicationUserIds.Any() || applicationUserIds.Contains((Guid)p.ApplicationUserId)) &&
                         (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId))).AsQueryable();
            return data;
        }

        public async Task<List<Guid?>> GetPersondIdsAsync(DateTime begTime, DateTime endTime, List<Guid> companyIds)
        {
            var persondIds = await GetDialogues(begTime, endTime, companyIds)
                    .Where ( p => p.PersonId != null )
                    .Select(p => p.PersonId).Distinct()
                    .ToListAsync();
            return persondIds;
        }

        public async Task<Guid> GetCrossPhraseTypeIdAsync()
        {
            var typeIdCross = await _context.PhraseTypes
                    .Where(p => p.PhraseTypeText == "Cross")
                    .Select(p => p.PhraseTypeId)
                    .FirstOrDefaultAsync();
            return typeIdCross;
        }
    }
}
