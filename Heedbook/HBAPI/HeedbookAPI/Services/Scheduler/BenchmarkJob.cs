using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBLib;
using HBLib.Utils;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Quartz;
using Microsoft.Azure;
using Microsoft.Extensions.DependencyInjection;
using HBData.Models;
using UserOperations.Models.AnalyticModels;
using UserOperations.Utils;

namespace UserOperations.Services.Scheduler
{
    public class BenchmarkJob : IJob
    {
        private RecordsContext _context;
        private DBOperations _dbOperation;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ElasticClientFactory _elasticClientFactory;

        public BenchmarkJob(IServiceScopeFactory scopeFactory, ElasticClientFactory elasticClientFactory)
        {
            _scopeFactory = scopeFactory;
            _elasticClientFactory = elasticClientFactory;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _log = _elasticClientFactory.GetElasticClient();
                try
                {
                    _context = scope.ServiceProvider.GetRequiredService<RecordsContext>();
                    _dbOperation = scope.ServiceProvider.GetRequiredService<DBOperations>();

                    for (int i = 0; i < 365; i++)
                    {

                    DateTime today = DateTime.Now.AddDays(-i).Date;
                   // if (!_context.Benchmarks.Any(x => x.Day == today))
                    {
                        FillIndexesForADay(today);
                      //  _log.Info("Calculation of benchmarks finished");
                    }
                    }
                }
                catch (Exception e)
                {
                    _log.Fatal($"{e}");
                    throw;
                }
            }
        }

        private void FillIndexesForADay(DateTime today)
        {
            var nextDay = today.AddDays(1);
            var typeIdCross = _context.PhraseTypes
                   .Where(p => p.PhraseTypeText == "Cross")
                   .Select(p => p.PhraseTypeId)
                   .First();
            var dialogues = _context.Dialogues
                     .Where(p => p.BegTime.Date == today
                             && p.StatusId == 3
                             && p.InStatistic == true
                             && p.ApplicationUser.Company.CompanyIndustryId != null)
                     .Select(p => new DialogueInfo
                     {
                         IndustryId = p.ApplicationUser.Company.CompanyIndustryId,
                         CompanyId = p.ApplicationUser.CompanyId,
                         DialogueId = p.DialogueId,
                         CrossCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdCross).Count(),
                         SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                         BegTime = p.BegTime,
                         EndTime = p.EndTime
                     })
                     .ToList();

            var sessions = _context.Sessions
                     .Where(p => p.BegTime.Date == today
                           && p.StatusId == 7)
                   .Select(p => new SessionInfo
                   {
                       IndustryId = p.ApplicationUser.Company.CompanyIndustryId,
                       CompanyId = p.ApplicationUser.CompanyId,
                       BegTime = p.BegTime,
                       EndTime = p.EndTime
                   })
                   .ToList();

            var benchmarkNames = _context.BenchmarkNames.ToList();
            var benchmarkSatisfIndustryAvgId = GetBenchmarkNameId("SatisfactionIndexIndustryAvg", benchmarkNames);
            var benchmarkSatisfIndustryMaxId = GetBenchmarkNameId("SatisfactionIndexIndustryBenchmark", benchmarkNames);

            var benchmarkCrossIndustryAvgId = GetBenchmarkNameId("CrossIndexIndustryAvg", benchmarkNames);
            var benchmarkCrossIndustryMaxId = GetBenchmarkNameId("CrossIndexIndustryBenchmark", benchmarkNames);

            var benchmarkLoadIndustryAvgId = GetBenchmarkNameId("LoadIndexIndustryAvg", benchmarkNames);
            var benchmarkLoadIndustryMaxId = GetBenchmarkNameId("LoadIndexIndustryBenchmark", benchmarkNames);

            if (dialogues.Count() != 0)
            {
                foreach (var groupIndustry in dialogues.GroupBy(x => x.IndustryId))
                {
                    var dialoguesInIndustry = groupIndustry.ToList();
                    var sessionsInIndustry = sessions.Where(x => x.IndustryId == groupIndustry.Key).ToList();
                    //  if (dialoguesInIndustry.Count() != 0 && sessionsInIndustry.Count() != 0)
                    {
                        var satisfactionIndex = _dbOperation.SatisfactionIndex(dialoguesInIndustry);
                        var crossIndex = _dbOperation.CrossIndex(dialoguesInIndustry);
                        var loadIndex = _dbOperation.LoadIndex(sessionsInIndustry, dialoguesInIndustry, today, nextDay);
                        AddNewBenchmark((double)satisfactionIndex, benchmarkSatisfIndustryAvgId, today, groupIndustry.Key);
                        AddNewBenchmark((double)crossIndex, benchmarkCrossIndustryAvgId, today, groupIndustry.Key);
                        AddNewBenchmark((double)loadIndex, benchmarkLoadIndustryAvgId, today, groupIndustry.Key);

                        var maxSatisfInd = dialoguesInIndustry.GroupBy(x => x.CompanyId).Max(x => _dbOperation.SatisfactionIndex(x.ToList()));
                        var maxCrossIndex = dialoguesInIndustry.GroupBy(x => x.CompanyId).Max(x => _dbOperation.CrossIndex(x.ToList()));
                        var maxLoadIndex = dialoguesInIndustry.GroupBy(x => x.CompanyId)
                            .Max(p =>
                            _dbOperation.LoadIndex(
                                sessionsInIndustry.Where(s => s.CompanyId == p.Key).ToList(),
                                p.ToList(),
                                today,
                                nextDay));

                        AddNewBenchmark((double)maxSatisfInd, benchmarkSatisfIndustryMaxId, today, groupIndustry.Key);
                        AddNewBenchmark((double)maxCrossIndex, benchmarkCrossIndustryMaxId, today, groupIndustry.Key);
                        AddNewBenchmark((double)maxLoadIndex, benchmarkLoadIndustryMaxId, today, groupIndustry.Key);
                    }
                }

                _context.SaveChanges();
            }
        }

        private void AddNewBenchmark(double val, Guid benchmarkNameId, DateTime today,  Guid? industryId = null)
        {
            Benchmark benchmark = new Benchmark()
            {
                IndustryId = industryId,
                Value = val,
                Weight = 1,// dialoguesInCompany.Count();
                Day = today,
                BenchmarkNameId = benchmarkNameId
            };
            _context.Benchmarks.Add(benchmark);
        }

        private Guid GetBenchmarkNameId(string name, List<BenchmarkName> benchmarkNames)
        {
            return benchmarkNames.FirstOrDefault(x => x.Name == name).Id;
        }
    }
}