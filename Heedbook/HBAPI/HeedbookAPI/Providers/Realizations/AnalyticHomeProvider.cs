using HBData.Models;
using HBData.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserOperations.Models.AnalyticModels;

namespace UserOperations.Providers
{
    public class AnalyticHomeProvider : IAnalyticHomeProvider
    {
        private readonly IGenericRepository _repository;
        public AnalyticHomeProvider(IGenericRepository repository)
        {
            _repository = repository;
        }

        //public IQueryable<DialogueHint> GetDialogueHints(DateTime begTime, DateTime endTime, List<Guid> companyIds, List<Guid> applicationUserIds, List<Guid> workerTypeIds)
        //{
        //    var hints = _context.DialogueHints
        //            .Include(p => p.Dialogue)
        //            .Include(p => p.Dialogue.ApplicationUser)
        //            .Where(p => p.Dialogue.BegTime >= begTime && p.Dialogue.EndTime <= endTime
        //                    && p.Dialogue.InStatistic == true
        //                    && (!companyIds.Any() || companyIds.Contains((Guid)p.Dialogue.ApplicationUser.CompanyId))
        //                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.Dialogue.ApplicationUserId))
        //                    && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.Dialogue.ApplicationUser.WorkerTypeId)))
        //           .AsQueryable();
        //    return hints;
        //}

        public async Task<IEnumerable<BenchmarkModel>> GetBenchmarksList(DateTime begTime, DateTime endTime, List<Guid> companyIds)
        {
            var industryIds = await GetIndustryIdsAsync(companyIds);
            try
            {
                var benchmarksList = _repository.Get<Benchmark>().Where(x => x.Day >= begTime && x.Day <= endTime
                                                             && industryIds.Contains(x.IndustryId))
                                                              .Join(_repository.Get<BenchmarkName>(),
                                                              bench => bench.BenchmarkNameId,
                                                              names => names.Id,
                                                              (bench, names) => new BenchmarkModel { Name = names.Name, Value = bench.Value });
                return benchmarksList;
            }
            catch
            {
                return null;
            }
        }

        public double? GetBenchmarkIndustryAvg(List<BenchmarkModel> benchmarksList, string banchmarkName)
        {
            if (benchmarksList == null || benchmarksList.Count() == 0) return null;
           return benchmarksList.Any(x => x.Name == banchmarkName) ? 
                (double?)benchmarksList.Where(x => x.Name == banchmarkName).Average(x => x.Value) : null;
        }

        public double? GetBenchmarkIndustryMax(List<BenchmarkModel> benchmarksList, string banchmarkName)
        {
            if (benchmarksList == null || benchmarksList.Count() == 0) return null;
            return benchmarksList.Any(x => x.Name == banchmarkName) ?
                 (double?)benchmarksList.Where(x => x.Name == banchmarkName).Max(x => x.Value) : null;
        }

        public async Task<IEnumerable<Guid?>> GetIndustryIdsAsync(List<Guid> companyIds)
        {
            var industryIds = (await _repository.FindByConditionAsync<Company>(x => !companyIds.Any() || companyIds.Contains(x.CompanyId)))?
                     .Select(x => x.CompanyIndustryId).Distinct();
            return industryIds;
        }

        public async Task<int> GetSessionOnline( List<Guid> companyIds, List<Guid> workerTypeIds)
        {
            return await _repository.GetAsQueryable<Session>().Where(p =>
                     p.StatusId == 6
                     && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                     && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId))).CountAsync();
        }
    }
}
