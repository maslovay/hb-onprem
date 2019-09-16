using HBData.Models;
using HBLib;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using HBLib.Utils;
using Newtonsoft.Json;
using System.Net.Mime;
using HBData.Repository;
using HBData;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using UserOperations.Models.AnalyticModels;
using UserOperations.Utils;

namespace UserOperations.Services
{
    public class BenchmarkFilling
    {
        private readonly IGenericRepository _repository;
        private readonly IConfiguration _config;
        private readonly RecordsContext _context;
        private readonly DBOperations _dbOperation;
        DateTime today;
        public BenchmarkFilling(
             IGenericRepository repository,
             IConfiguration config, 
             RecordsContext context,
             DBOperations dbOperation)
        {
            _repository = repository;
            _config = config;
            _context = context;
            _dbOperation = dbOperation;
            today = DateTime.Now.AddDays(-7).Date;
        }

        public void FillIndexesForADay()
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
            var benchmarkSatisfTotalAvgId = GetBenchmarkNameId("SatisfactionIndexTotalAvg", benchmarkNames);
            var benchmarkSatisfIndustryAvgId = GetBenchmarkNameId("SatisfactionIndexIndustryAvg", benchmarkNames);
            var benchmarkSatisfIndustryMaxId = GetBenchmarkNameId("SatisfactionIndexIndustryBenchmark", benchmarkNames); 

            var benchmarkCrossTotalAvgId = GetBenchmarkNameId("CrossIndexTotalAvg", benchmarkNames);
            var benchmarkCrossIndustryAvgId = GetBenchmarkNameId("CrossIndexIndustryAvg", benchmarkNames); 
            var benchmarkCrossIndustryMaxId = GetBenchmarkNameId("CrossIndexIndustryBenchmark", benchmarkNames); 

            var benchmarkLoadTotalAvgId = GetBenchmarkNameId("LoadIndexTotalAvg", benchmarkNames);
            var benchmarkLoadIndustryAvgId = GetBenchmarkNameId("LoadIndexIndustryAvg", benchmarkNames); 
            var benchmarkLoadIndustryMaxId = GetBenchmarkNameId("LoadIndexIndustryBenchmark", benchmarkNames);

            if (dialogues.Count() != 0)
            {
                var satisfactionIndexTotal = _dbOperation.SatisfactionIndex(dialogues);
                var crossIndexTotal = _dbOperation.CrossIndex(dialogues);
                //  var loadIndex = _dbOperation.LoadIndex(sessions, dialogues, today, today.AddDays(1));
                var loadIndexTotal = _dbOperation.LoadIndex(sessions, dialogues, today, nextDay);
                AddNewBenchmark((double)satisfactionIndexTotal, benchmarkSatisfTotalAvgId);
                AddNewBenchmark((double)crossIndexTotal, benchmarkCrossTotalAvgId);
                AddNewBenchmark((double)loadIndexTotal, benchmarkLoadTotalAvgId);


                foreach (var groupIndustry in dialogues.GroupBy(x => x.IndustryId))
                {
                    var dialoguesInIndustry = groupIndustry.ToList();
                    var sessionsInIndustry = sessions.Where(x => x.IndustryId == groupIndustry.Key).ToList();
                    //  if (dialoguesInIndustry.Count() != 0 && sessionsInIndustry.Count() != 0)
                    {
                        var satisfactionIndex = _dbOperation.SatisfactionIndex(dialoguesInIndustry);
                        var crossIndex = _dbOperation.CrossIndex(dialoguesInIndustry);
                        var loadIndex = _dbOperation.LoadIndex(sessionsInIndustry, dialoguesInIndustry, today, nextDay);
                        AddNewBenchmark((double)satisfactionIndex, benchmarkSatisfIndustryAvgId, groupIndustry.Key);
                        AddNewBenchmark((double)crossIndex, benchmarkCrossIndustryAvgId, groupIndustry.Key);
                        AddNewBenchmark((double)loadIndex, benchmarkLoadIndustryAvgId, groupIndustry.Key);

                        var maxSatisfInd = dialoguesInIndustry.GroupBy(x => x.CompanyId).Max(x => _dbOperation.SatisfactionIndex(x.ToList()));
                        var maxCrossIndex = dialoguesInIndustry.GroupBy(x => x.CompanyId).Max(x => _dbOperation.CrossIndex(x.ToList()));
                        var maxLoadIndex = dialoguesInIndustry.GroupBy(x => x.CompanyId)
                            .Max(p =>
                            _dbOperation.LoadIndex(
                                sessionsInIndustry.Where(s => s.CompanyId == p.Key).ToList(),
                                p.ToList(),
                                today,
                                nextDay));

                        AddNewBenchmark((double)maxSatisfInd, benchmarkSatisfIndustryMaxId, groupIndustry.Key);
                        AddNewBenchmark((double)maxCrossIndex, benchmarkCrossIndustryMaxId, groupIndustry.Key);
                        AddNewBenchmark((double)maxLoadIndex, benchmarkLoadIndustryMaxId, groupIndustry.Key);
                    }
                }

                _context.SaveChanges();
            }
        }

        private void AddNewBenchmark(double val, Guid benchmarkNameId, Guid? industryId = null)
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
