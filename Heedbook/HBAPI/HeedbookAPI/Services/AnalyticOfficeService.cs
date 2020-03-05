using System;
using System.Collections.Generic;
using System.Linq;
using UserOperations.Utils;
using UserOperations.Utils.AnalyticOfficeUtils;
using Newtonsoft.Json;
using HBData.Repository;
using HBData.Models;
using UserOperations.Models.AnalyticModels;
using System.Threading.Tasks;
using UserOperations.Controllers;
using UserOperations.Models.Get.HomeController;

namespace UserOperations.Services
{
    public class AnalyticOfficeService
    {  
        private readonly LoginService _loginService;
        private readonly RequestFilters _requestFilters;
        private readonly IGenericRepository _repository;
        private readonly AnalyticOfficeUtils _utils;
        private readonly DBOperations _dbOperations;

        public AnalyticOfficeService(
            LoginService loginService,
            RequestFilters requestFilters,
            IGenericRepository repository,
            AnalyticOfficeUtils analyticOfficeUtils,
            DBOperations dbOperations
            )
        {
            _loginService = loginService;
            _requestFilters = requestFilters;
            _repository = repository;
            _utils = analyticOfficeUtils;
            _dbOperations = dbOperations;
        }
        public async Task<string> Efficiency(string beg, string end,
                                        List<Guid?> applicationUserIds, List<Guid> companyIds, List<Guid> corporationIds, List<Guid> deviceIds)
        {
                var role = _loginService.GetCurrentRoleName();
                var companyId = _loginService.GetCurrentCompanyId();
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);
                var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);

                var workingTimes = _repository.GetAsQueryable<WorkingTime>().Where(x => !companyIds.Any() || companyIds.Contains(x.CompanyId)).ToArray();

                var sessions = GetSessionsInfo(prevBeg, endTime, companyIds, applicationUserIds, deviceIds);
                var sessionCur = sessions.Where(p => p.BegTime.Date >= begTime).ToList();
                var sessionOld = sessions.Where(p => p.BegTime.Date < begTime).ToList();

                List<DialogueInfo> dialogues = GetDialoguesInfo(prevBeg, endTime, companyIds, applicationUserIds, deviceIds, workingTimes);
                var dialoguesCur = dialogues.Where(p => p.BegTime >= begTime).ToList();
                var dialoguesOld = dialogues.Where(p => p.BegTime < begTime).ToList();

                var dialoguesUserCur = dialoguesCur.Where(p => p.ApplicationUserId != null).ToList();
                var dialoguesUserOld = dialoguesOld.Where(p => p.ApplicationUserId != null).ToList();

                var dialoguesDevicesCur = dialoguesCur.Where(x => x.IsInWorkingTime).ToList();
                var dialoguesDevicesOld = dialoguesOld.Where(x => x.IsInWorkingTime).ToList();
                var timeTableForDevices = WorkingDaysTimeListInMinutes(workingTimes, begTime, endTime, companyIds, role);
                List<BenchmarkModel> benchmarksList = (await GetBenchmarksList(begTime, endTime, companyIds)).ToList();

            var result = new EfficiencyDashboardInfoNew
            {
                WorkloadValueAvg = _utils.LoadIndex(sessionCur, dialoguesUserCur, begTime, endTime),
                WorkloadDynamics = -_utils.LoadIndex(sessionOld, dialoguesUserOld, prevBeg, begTime),
                DialoguesCount = _utils.DialoguesCount(dialoguesCur),
                AvgWorkingTime = _utils.SessionAverageHours(sessionCur, begTime, endTime),
                AvgDurationDialogue = _utils.DialogueAverageDuration(dialoguesCur, begTime, endTime),
                WorkloadValueAvgByWorkingTime = _dbOperations.WorklLoadByTimeIndex(timeTableForDevices, dialoguesDevicesCur, begTime, endTime),
                WorkloadDynamicsWorkingTime = - _dbOperations.WorklLoadByTimeIndex(timeTableForDevices, dialoguesDevicesOld, prevBeg, begTime)
            };

            if (benchmarksList != null && benchmarksList.Count() != 0)
            {
                result.LoadIndexIndustryAverage = GetBenchmarkIndustryAvg(benchmarksList, "LoadIndexIndustryAvg");
                result.LoadIndexIndustryBenchmark = GetBenchmarkIndustryMax(benchmarksList, "LoadIndexIndustryBenchmark");

                result.WorkLoadByTimeIndustryAverage = GetBenchmarkIndustryAvg(benchmarksList, "WorkLoadByTimeIndustryAvg");
                result.WorkLoadByTimeIndustryBenchmark = GetBenchmarkIndustryMax(benchmarksList, "WorkLoadByTimeIndustryBenchmark");
            }

            var satisfactionIndex = _utils.SatisfactionIndex(dialoguesCur);
                var loadIndex = _utils.LoadIndex(sessionCur, dialoguesUserCur, begTime, endTime.AddDays(1));
                var employeeCount = _utils.EmployeeCount(dialoguesUserCur);
                var deviceCount = _utils.DeviceCount(dialoguesCur);
            //   result.CorrelationLoadSatisfaction = satisfactionIndex != 0?  loadIndex / satisfactionIndex : 0;
                result.WorkloadDynamics += result.WorkloadValueAvg;
                result.WorkloadDynamicsWorkingTime += result.WorkloadValueAvgByWorkingTime;
                result.DialoguesNumberAvgPerEmployee = (dialoguesUserCur.Count() != 0 && employeeCount != 0) ? dialoguesUserCur.GroupBy(p => p.BegTime.Date).Select(p => p.Count()).Average() / employeeCount : 0;
                result.DialoguesNumberAvgPerDevice = (dialoguesCur.Count() != 0) ? dialoguesCur.GroupBy(p => p.BegTime.Date).Select(p => p.Count()).Average() / deviceCount : 0;
                result.DialoguesNumberAvgPerDayOffice = (dialoguesCur.Count() != 0) ? dialoguesCur.GroupBy(p => p.BegTime.Date).Select(p => p.Count()).Average() : 0;

            var optimalLoad = 0.7;
                var employeeWorked = sessionCur
                .GroupBy(p => p.BegTime.Date)
                .Select(p => new
                {
                    Day = p.Key.ToString(),
                    EmployeeCount = _utils
                        .EmployeeCount(
                            dialoguesCur.Where(x => x.BegTime >= p.Min(s => s.BegTime) && x.EndTime < p.Max(s => s.EndTime)).ToList()
                           ),
                    LoadIndex = _utils
                        .LoadIndex(
                            p.ToList(), 
                            dialoguesCur.Where(x => x.BegTime >= p.Min(s => s.BegTime) && x.EndTime < p.Max(s => s.EndTime)).ToList(),
                            p.Min(s => s.BegTime),
                            p.Max(s => s.EndTime))
                }).ToList();

                var diagramEmployeeWorked = employeeWorked.Select(
                    p => new {
                        p.Day,
                        p.EmployeeCount,
                        EmployeeOptimalCount = (p.LoadIndex != null & p.LoadIndex != 0) ?
                        (Int32?)(Math.Ceiling((double)(p.EmployeeCount * optimalLoad / p.LoadIndex))) : null
                    }
                );

                var clientTime = dialoguesCur
                   .GroupBy(p => p.BegTime.Hour)
                   .Select(p => new EfficiencyLoadClientTimeInfo
                   {
                       Time = $"{p.Key}:00",
                       ClientCount = p.GroupBy(q => q.BegTime.Date)
                       .Average(q => q.Count())
                   }).ToList();

                var days = new List<string> {"Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
                var clientDay = dialoguesCur?
                    .GroupBy(p => p.BegTime.DayOfWeek)
                    .Select(p => new EfficiencyLoadClientDayInfo
                    {
                        Day = p.Key.ToString(),
                        ClientCount = p.GroupBy(q => q.BegTime.Date)
                            .Average(q => q.Count())
                    }).ToList();
                var zeroDays = days.Where(x => !clientDay.Select(p => p.Day).Contains(x))
                        .Select(p => new EfficiencyLoadClientDayInfo
                        {
                            Day = p,
                            ClientCount = 0
                        });
                clientDay = clientDay.Union(zeroDays).ToList();


            //----new diagrams---dialogue amount by device and by employee
            var dialogueUserDate = dialoguesUserCur?
                 .GroupBy(p => p.BegTime.Date)
                 .OrderBy(p => p.Key)
                 .Select(p => new 
                 {
                     Day = p.Key.ToShortDateString(),
                     DialoguesUsers = p.GroupBy(r => r.ApplicationUserId)
                     .Select(r => new 
                                    {
                                        UserId = (Guid)r.Key,
                                        ClientCount = r.Count(),
                                        r.FirstOrDefault()?.FullName
                                    }).OrderBy(r => r.UserId).ToArray()
                 }).ToArray();


            var dialogueDeviceDate = dialoguesDevicesCur?
                 .GroupBy(p => p.BegTime.Date)
                 .OrderBy(p => p.Key)
                 .Select(p => new
                 {
                     Day = p.Key.ToShortDateString(),
                     DialoguesDevices = p.GroupBy(r => r.DeviceId)
                     .Select(r => new
                     {
                         DeviceId = r.Key,
                         ClientCount = r.Count(),
                         r.FirstOrDefault()?.DeviceName,
                     }).OrderBy(r => r.DeviceId).ToArray()
                 }).ToArray();
            //---end new block

            var pauseInMin = DialogueAvgPauseListInMinutes(workingTimes, dialoguesDevicesCur, begTime, endTime, role, companyIds);

            if (pauseInMin == null) pauseInMin = timeTableForDevices;
                var sessTimeMinutes = timeTableForDevices.Sum();
            ///----fix difference!!!!
            var totalDialogueDur = dialoguesDevicesCur.Sum(x => x.EndTime.Subtract(x.BegTime).TotalMinutes);
            var fract =  (sessTimeMinutes - totalDialogueDur)/ pauseInMin.Sum() ;
            if (Math.Abs(fract - 1) > 0.01)
            {
                pauseInMin = pauseInMin.Select(x => x * fract).ToList();
            }


                var pausesAmount = new{
                    Less_10 = pauseInMin?.Where(p => p <= 10).Count(),
                    Between_11_20 = pauseInMin?.Where(p => p > 10 && p <= 20).Count(),
                    Between_21_60 = pauseInMin?.Where(p => p > 20 && p <= 60).Count(),
                    More_60 = pauseInMin?.Where(p => p > 60).Count()
                };

                var pausesShareInSession = new{
                    Less_10 = sessTimeMinutes != 0 && pauseInMin != null? 100 *  pauseInMin.Where(p => p <= 10).Sum() / sessTimeMinutes : 0,
                    Between_11_20 = sessTimeMinutes != 0 && pauseInMin != null ? 100 * pauseInMin.Where(p => p > 10 && p <= 20).Sum() / sessTimeMinutes : 0,
                    Between_21_60 = sessTimeMinutes != 0 && pauseInMin != null ? 100 * pauseInMin.Where(p => p > 20 && p <= 60).Sum() / sessTimeMinutes : 0,
                    More_60 = sessTimeMinutes != 0 && pauseInMin != null ? 100 * pauseInMin.Where(p => p > 60).Sum() / sessTimeMinutes : 0,
                    Load =  sessTimeMinutes != 0 && pauseInMin != null ? Math.Round(100 * (double)(sessTimeMinutes - pauseInMin.Sum()) / sessTimeMinutes, 2) : 0
                };
                 var pausesInMinutes = new{
                    Less_10 = pauseInMin?.Where(p => p <= 10).Sum(),
                    Between_11_20 = pauseInMin?.Where(p => p > 10 && p <= 20).Sum(),
                    Between_21_60 = pauseInMin?.Where(p => p > 20 && p <= 60).Sum() ,
                    More_60 = pauseInMin?.Where(p => p > 60).Sum(),
                    Load = pauseInMin != null? sessTimeMinutes - Math.Round((double)pauseInMin?.Sum(), 2) : sessTimeMinutes
                };

           
  
                var jsonToReturn = new Dictionary<string, object>();
                jsonToReturn["Workload"] = result;
              //  jsonToReturn["DiagramDialogDurationPause"] = diagramDialogDurationPause;
                jsonToReturn["DiagramEmployeeWorked"] = diagramEmployeeWorked;
                jsonToReturn["ClientTime"] = clientTime;
                jsonToReturn["ClientDay"] = clientDay;
                jsonToReturn["DialogueUserDate"] = dialogueUserDate;
                jsonToReturn["DialogueDeviceDate"] = dialogueDeviceDate;
                jsonToReturn["PausesAmount"] = pausesAmount;
                jsonToReturn["PausesShare"] = pausesShareInSession;
                jsonToReturn["PausesInMinutes"] = pausesInMinutes;
                return JsonConvert.SerializeObject(jsonToReturn);
        }

        //---PRIVATE---

        private List<SessionInfo> GetSessionsInfo(
        DateTime prevBeg,
        DateTime endTime,
        List<Guid> companyIds,
        List<Guid?> applicationUserIds,
        List<Guid> deviceIds)
        {
            var sessions = _repository.GetAsQueryable<Session>().Where(
                    p => p.BegTime >= prevBeg
                    && p.EndTime <= endTime
                    && p.StatusId == 7
                    && (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                    && (!deviceIds.Any() || deviceIds.Contains(p.DeviceId)))
                .Select(p => new SessionInfo
                {
                    ApplicationUserId = p.ApplicationUserId,
                    DeviceId = p.DeviceId,
                    BegTime = p.BegTime,
                    EndTime = p.EndTime
                })
                .ToList();
            return sessions;
        }
        private List<DialogueInfo> GetDialoguesInfo(
            DateTime prevBeg,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid?> applicationUserIds,
            List<Guid> deviceIds,
            WorkingTime[] workingTimes)
        {
            var dialogues = _repository.GetAsQueryable<Dialogue>().Where(
                    p => p.BegTime >= prevBeg
                    && p.EndTime <= endTime
                    && p.StatusId == 3
                    && p.InStatistic == true
                    && (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                    && (!deviceIds.Any() || deviceIds.Contains(p.DeviceId)))
                    .Select(p =>  new DialogueInfo
                    {
                        DialogueId = p.DialogueId,
                        ApplicationUserId = p.ApplicationUserId,
                        DeviceId = p.DeviceId,
                        DeviceName = p.Device.Name,
                        CompanyId = p.Device.CompanyId,
                        BegTime = p.BegTime,
                        EndTime = p.EndTime,
                        FullName = p.ApplicationUser.FullName,
                        SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                        IsInWorkingTime = _utils.CheckIfDialogueInWorkingTime(p, workingTimes.Where(x => x.CompanyId == p.Device.CompanyId).ToArray())
                    })
                .ToList();
            return dialogues;
        }

        private double TimetableHoursForAllComapnies(string role, DateTime beg, DateTime end, List<Guid> companyIds, List<Guid> deviceIds)
        {
            if (role == "Admin") return 0;
            return companyIds.Sum(x => TimetableHours(beg, end, x, deviceIds));
        }

        private double TimetableHours(DateTime beg, DateTime end, Guid companyId, List<Guid> deviceIds)
        {
            int active = 3;
            var timeTable = GetTimeTable(companyId);
            var devicesAmount = _repository.GetAsQueryable<Device>()
                .Where(x => x.CompanyId == companyId && x.StatusId == active
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

            var timeTable =  _repository.GetAsQueryable<WorkingTime>().Where(x => x.CompanyId == companyId)
                    .OrderBy(x => x.Day).Select(x => CalcWorkingDayDurationMin(x.BegTime, x.EndTime)).ToArray();
            if (timeTable == null || timeTable.Count() < 7) throw new NoDataException("company has no timetable");
            return timeTable;
        }

        private double CalcWorkingDayDurationMin(DateTime? beg, DateTime? end)
        {
            if (beg == null || end == null)
                return 0;
            var timeStartWorkingDay = DateTime.Now.Date.AddHours(((DateTime)beg).Hour).AddMinutes(((DateTime)beg).Minute);
            var timeEndWorkingDay = DateTime.Now.Date.AddHours(((DateTime)end).Hour).AddMinutes(((DateTime)end).Minute);
            return timeEndWorkingDay.Subtract(timeStartWorkingDay).TotalMinutes;
        }

        private async Task<IEnumerable<BenchmarkModel>> GetBenchmarksList(DateTime begTime, DateTime endTime, List<Guid> companyIds)
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


        private double? GetBenchmarkIndustryAvg(List<BenchmarkModel> benchmarksList, string banchmarkName)
        {
            if (benchmarksList == null || benchmarksList.Count() == 0) return null;
            return benchmarksList.Any(x => x.Name == banchmarkName) ?
                 (double?)benchmarksList.Where(x => x.Name == banchmarkName).Average(x => x.Value) : null;
        }

        private double? GetBenchmarkIndustryMax(List<BenchmarkModel> benchmarksList, string banchmarkName)
        {
            if (benchmarksList == null || benchmarksList.Count() == 0) return null;
            return benchmarksList.Any(x => x.Name == banchmarkName) ?
                 (double?)benchmarksList.Where(x => x.Name == banchmarkName).Max(x => x.Value) : null;
        }

        private async Task<IEnumerable<Guid?>> GetIndustryIdsAsync(List<Guid> companyIds)
        {
            var industryIds = (await _repository.FindByConditionAsync<Company>(x => !companyIds.Any() || companyIds.Contains(x.CompanyId)))?
                     .Select(x => x.CompanyIndustryId).Distinct();
            return industryIds;
        }

        public List<double> WorkingDaysTimeListInMinutes(WorkingTime[] timeTable, DateTime beg, DateTime end, List<Guid> companyIds, string role)
        {
            int active = 3;
            List<double> times = new List<double>();
            if (role == "Admin") return times;

            if (!timeTable.Any()) return null;
                foreach (var companyId in companyIds)
                {
                    var devicesAmount = _repository.GetAsQueryable<Device>().Where(x => x.CompanyId == companyId && x.StatusId == active).Count();
                    if (devicesAmount == 0) continue;
                    var timeTableForComp = GetTimeTable(companyId);
                    for (int d = 0; d < devicesAmount; d++)
                    {
                        for (var i = beg.Date; i < end.Date; i = i.AddDays(1))
                        {
                                times.Add(timeTableForComp[(int)i.DayOfWeek]);
                        }
                    }
                }
                return times;
        }

        public List<double> DialogueAvgPauseListInMinutes(WorkingTime[] timeTable, List<DialogueInfo> dialogues, DateTime beg, DateTime end, string role, List<Guid> companyIds)
        {
            int active = 3;
            List<double> pauses = new List<double>();
            if (!timeTable.Any() || !dialogues.Any()) return null;
            if (role == "Admin") return pauses;

            foreach (var companyId in companyIds)
            {
                var devices = _repository.GetAsQueryable<Device>().Where(x => x.CompanyId == companyId && x.StatusId == active).Select(x => x.DeviceId).ToList();
                if (devices.Count() == 0) continue;
                foreach (var devId in devices)
                {
                    for (var i = beg.Date; i < end.Date; i = i.AddDays(1))
                    {
                        try
                        {
                            var endDay = i.AddDays(1);
                            var workingHours = timeTable.Where(x => x.CompanyId == companyId && x.Day == (int)i.DayOfWeek).FirstOrDefault();
                            if (workingHours?.BegTime == null) continue;
                            var dialogInDay = dialogues.Where(p => p.DeviceId == devId && p.BegTime >= i && p.EndTime <= endDay)
                                          .OrderBy(p => p.BegTime).ToArray();
                                 

                            List<DateTime> times = new List<DateTime>();
                            var timeStartWorkingDay = i.AddHours(((DateTime)workingHours.BegTime).Hour).AddMinutes(((DateTime)workingHours.BegTime).Minute);
                            var timeEndWorkingDay = i.AddHours(((DateTime)workingHours.EndTime).Hour).AddMinutes(((DateTime)workingHours.EndTime).Minute);
                            if (!dialogInDay.Any())
                            {
                                pauses.Add(timeEndWorkingDay.Subtract(timeStartWorkingDay).TotalMinutes);
                                continue;
                            }


                            times.Add(timeStartWorkingDay);
                            for (var j = 0; j < dialogInDay.Count(); j++)
                            {
                                if (j == 0 || dialogInDay[j].BegTime >= dialogInDay[j - 1].EndTime)
                                {
                                    times.Add(dialogInDay[j].BegTime);
                                    times.Add(dialogInDay[j].EndTime);
                                }
                                else
                                {
                                    times.Remove(times.Last());
                                    times.Add(dialogInDay[j].EndTime);
                                }
                            }
                            times.Add(timeEndWorkingDay);

                            for (int j = 0; j < times.Count() - 1; j += 2)
                            {
                                var pause = (times[j + 1].Subtract(times[j])).TotalMinutes;
                                pauses.Add(pause < 0 ? 0 : pause);
                            }
                        }
                        catch { }
                    }
                }
                }
         return pauses;
        }

    }
}