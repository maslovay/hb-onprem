using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBLib;
using Quartz;
using Microsoft.Extensions.DependencyInjection;
using HBData.Models;
using UserOperations.Models.AnalyticModels;
using UserOperations.Utils;
using UserOperations.Controllers;

namespace BenchmarkScheduler
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

                    for (int i = 1; i < 6; i++)
                    {
                        DateTime today = DateTime.Now.AddDays(-i).Date;
                        if (!_context.Benchmarks.Any(x => x.Day.Date == today))
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
            var workingTimes = _context.WorkingTimes.ToList();

            var dialogues = _context.Dialogues
                     .Where(p => p.BegTime.Date == today
                             && p.StatusId == 3
                             && p.InStatistic == true
                             && p.Device.Company.CompanyIndustryId != null)
                     .Select(p => new DialogueInfoFull
                     {
                         IndustryId = p.Device.Company.CompanyIndustryId,
                         CompanyId = p.Device.CompanyId,
                         DialogueId = p.DialogueId,
                         CrossCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdCross).Count(),
                         SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                         BegTime = p.BegTime,
                         EndTime = p.EndTime,
                         IsInWorkingTime = _dbOperation.CheckIfDialogueInWorkingTime(p, workingTimes.Where(x => x.CompanyId == p.Device.CompanyId).ToArray()),
                         DeviceId = p.DeviceId,
                         ApplicationUserId = p.ApplicationUserId
                     })
                     .ToList();

            var timeTableForDevices = TimetableHoursForAllComapnies(today, nextDay, dialogues.Select(x => (Guid)x.CompanyId).Distinct().ToList(), dialogues.Select(x => x.DeviceId).Distinct().ToList());

            var sessions = _context.Sessions
                     .Where(p => p.BegTime.Date == today
                           && p.StatusId == 7)
                   .Select(p => new SessionInfo
                   {
                       IndustryId = p.Device.Company.CompanyIndustryId,
                       CompanyId = p.Device.CompanyId,
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

            var benchmarkWorkLoadByTimeIndustryAvgId = GetBenchmarkNameId("WorkLoadByTimeIndustryAvg", benchmarkNames);
            var benchmarkWorkLoadByTimeIndustryMaxId = GetBenchmarkNameId("WorkLoadByTimeIndustryBenchmark", benchmarkNames);

            if (dialogues.Count() != 0)
            {
                foreach (var groupIndustry in dialogues.GroupBy(x => x.IndustryId))
                {
                    var dialoguesInIndustry = groupIndustry.ToList();
                    var dialoguesInIndustryPerUser = groupIndustry.Where(x => x.ApplicationUserId != null).ToList();
                    var sessionsInIndustry = sessions.Where(x => x.IndustryId == groupIndustry.Key).ToList();
                    //  if (dialoguesInIndustry.Count() != 0 && sessionsInIndustry.Count() != 0)
                    {
                        var satisfactionIndex = _dbOperation.SatisfactionIndex(dialoguesInIndustry);
                        var crossIndex = _dbOperation.CrossIndex(dialoguesInIndustry);
                        var loadIndex = _dbOperation.LoadIndex(sessionsInIndustry, dialoguesInIndustryPerUser, today, nextDay);
                        var workloadAvgByWorkingTime = _dbOperation.WorklLoadByTimeIndex(timeTableForDevices, dialoguesInIndustry.Where(x => x.IsInWorkingTime).ToList(), today, nextDay);

                        if (satisfactionIndex != null) AddNewBenchmark((double)satisfactionIndex, benchmarkSatisfIndustryAvgId, today, groupIndustry.Key);
                        if (crossIndex != null) AddNewBenchmark((double)crossIndex, benchmarkCrossIndustryAvgId, today, groupIndustry.Key);
                        if (loadIndex != null) AddNewBenchmark((double)loadIndex, benchmarkLoadIndustryAvgId, today, groupIndustry.Key);
                        if (workloadAvgByWorkingTime != null) AddNewBenchmark((double)workloadAvgByWorkingTime, benchmarkWorkLoadByTimeIndustryAvgId, today, groupIndustry.Key);

                        var maxSatisfInd = dialoguesInIndustry.GroupBy(x => x.CompanyId).Max(x => _dbOperation.SatisfactionIndex(x.ToList()));
                        var maxCrossIndex = dialoguesInIndustry.GroupBy(x => x.CompanyId).Max(x => _dbOperation.CrossIndex(x.ToList()));
                        var maxLoadIndex = dialoguesInIndustryPerUser.GroupBy(x => x.CompanyId)
                            .Max(p =>
                            _dbOperation.LoadIndex(
                                sessionsInIndustry.Where(s => s.CompanyId == p.Key).ToList(),
                                p.ToList(),
                                today,
                                nextDay));
                        var maxWorkloadAvgByWorkingTime = dialoguesInIndustry.Where(x => x.IsInWorkingTime).GroupBy(x => x.CompanyId).Max(x => _dbOperation.WorklLoadByTimeIndex(timeTableForDevices, x.ToList()));

                        if (maxSatisfInd != null) AddNewBenchmark((double)maxSatisfInd, benchmarkSatisfIndustryMaxId, today, groupIndustry.Key);
                        if (maxCrossIndex != null) AddNewBenchmark((double)maxCrossIndex, benchmarkCrossIndustryMaxId, today, groupIndustry.Key);
                        if (maxLoadIndex != null) AddNewBenchmark((double)maxLoadIndex, benchmarkLoadIndustryMaxId, today, groupIndustry.Key);
                        if (maxWorkloadAvgByWorkingTime != null) AddNewBenchmark((double)maxWorkloadAvgByWorkingTime, benchmarkWorkLoadByTimeIndustryMaxId, today, groupIndustry.Key);
                    }
                }

                _context.SaveChanges();
            }
        }

        private void AddNewBenchmark(double val, Guid benchmarkNameId, DateTime today, Guid? industryId = null)
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

        private double TimetableHoursForAllComapnies(DateTime beg, DateTime end, List<Guid> companyIds, List<Guid> deviceIds)
        {
            return companyIds.Sum(x => TimetableHours(beg, end, x, deviceIds));
        }


        private double TimetableHours(DateTime beg, DateTime end, Guid companyId, List<Guid> deviceIds)
        {
            var timeTable = GetTimeTable(companyId);
            var devicesAmount = _context.Devices
                .Where(x => x.CompanyId == companyId
                && (deviceIds == null || deviceIds.Count() == 0 || deviceIds.Contains(x.DeviceId)))
                .Count();
            double totalHours = 0;
            for (var i = beg.Date; i < end.Date; i = i.AddDays(1))
            {
                totalHours += timeTable[(int)i.DayOfWeek];
            }
            return totalHours * devicesAmount;
        }

        private double[] GetTimeTable(Guid companyId)
        {
            var timeTable = _context.WorkingTimes.Where(x => x.CompanyId == companyId)
                    .OrderBy(x => x.Day).Select(x => x.EndTime != null ? ((DateTime)x.EndTime).Subtract((DateTime)x.BegTime).TotalHours : 0).ToArray();
            if (timeTable == null || timeTable.Count() < 7) throw new NoDataException("company has no timetable");
            return timeTable;
        }

    }
}