using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using Microsoft.EntityFrameworkCore;

namespace UserOperations.Providers
{
    public class AnalyticClientProfileProvider : IAnalyticClientProfileProvider
    {
        private readonly IGenericRepository _repository;
        public AnalyticClientProfileProvider(IGenericRepository repository)
        {
            _repository = repository;
        }
        public async Task<List<Guid?>> GetPersondIdsAsync(DateTime begTime, DateTime endTime, List<Guid> companyIds)
        {
            var persondIds = await GetDialogues(begTime, endTime, companyIds)
                    .Where ( p => p.PersonId != null )
                    .Select(p => p.PersonId).Distinct()
                    .ToListAsyncSafe();
            return persondIds;
        }
        private IQueryable<Dialogue> GetDialogues(DateTime begTime, DateTime endTime, List<Guid> companyIds = null, List<Guid> applicationUserIds = null, List<Guid> workerTypeIds = null)
        {
            var data = _repository.GetAsQueryable<Dialogue>()
                    .Where(p => p.BegTime >= begTime &&
                        p.EndTime <= endTime &&
                        p.StatusId == 3 &&
                        p.InStatistic == true &&
                        (companyIds == null || (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))) &&
                        (applicationUserIds == null || (!applicationUserIds.Any() || applicationUserIds.Contains((Guid)p.ApplicationUserId))) &&
                        (workerTypeIds == null || (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)))).AsQueryable();
            return data;
        }
        public IQueryable<Dialogue> GetDialoguesIncludedClientProfile(DateTime begTime, DateTime endTime, List<Guid> companyIds, List<Guid> applicationUserIds, List<Guid> workerTypeIds)
        {
            var data = _repository.GetAsQueryable<Dialogue>()
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
    }
}