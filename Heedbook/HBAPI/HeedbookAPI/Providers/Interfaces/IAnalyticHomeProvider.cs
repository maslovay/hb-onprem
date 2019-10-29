using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserOperations.Models.AnalyticModels;

namespace UserOperations.Providers
{
    public interface IAnalyticHomeProvider
    {
        Task<IEnumerable<BenchmarkModel>> GetBenchmarksList(DateTime begTime, DateTime endTime, List<Guid> companyIds);
        double? GetBenchmarkIndustryAvg(List<BenchmarkModel> benchmarksList, string banchmarkName);
        double? GetBenchmarkIndustryMax(List<BenchmarkModel> benchmarksList, string banchmarkName);
        Task<IEnumerable<Guid?>> GetIndustryIdsAsync(List<Guid> companyIds);
    }
}
