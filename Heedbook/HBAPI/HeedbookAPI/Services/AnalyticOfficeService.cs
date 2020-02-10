using System;
using System.Collections.Generic;
using System.Linq;
using UserOperations.Utils;
using UserOperations.Utils.AnalyticOfficeUtils;
using Newtonsoft.Json;
using HBData.Repository;
using HBData.Models;
using UserOperations.Models.AnalyticModels;

namespace UserOperations.Services
{
    public class AnalyticOfficeService
    {  
        private readonly LoginService _loginService;
        private readonly RequestFilters _requestFilters;
        private readonly IGenericRepository _repository;
        private readonly AnalyticOfficeUtils _analyticOfficeUtils;

        public AnalyticOfficeService(
            LoginService loginService,
            RequestFilters requestFilters,
            IGenericRepository repository,
            AnalyticOfficeUtils analyticOfficeUtils
            )
        {
            _loginService = loginService;
            _requestFilters = requestFilters;
            _repository = repository;
            _analyticOfficeUtils = analyticOfficeUtils;
        }
        public string Efficiency(string beg, string end,
                                        List<Guid?> applicationUserIds, List<Guid> companyIds, List<Guid> corporationIds, List<Guid> deviceIds)
        {
                var role = _loginService.GetCurrentRoleName();
                var companyId = _loginService.GetCurrentCompanyId();
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);
                var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);

                var sessions = GetSessionsInfo(prevBeg, endTime, companyIds, applicationUserIds, deviceIds);

                var sessionCur = sessions.Where(p => p.BegTime.Date >= begTime).ToList();
                var sessionOld = sessions.Where(p => p.BegTime.Date < begTime).ToList();
                List<DialogueInfo> dialogues = GetDialoguesInfo(prevBeg, endTime, companyIds, applicationUserIds, deviceIds);
                var dialoguesCur = dialogues.Where(p => p.BegTime >= begTime).ToList();
                var dialoguesOld = dialogues.Where(p => p.BegTime < begTime).ToList();

                var result = new EfficiencyDashboardInfoNew
                {
                    WorkloadValueAvg = _analyticOfficeUtils.LoadIndex(sessionCur, dialoguesCur, begTime, endTime),
                    WorkloadDynamics = -_analyticOfficeUtils.LoadIndex(sessionOld, dialoguesOld, prevBeg, begTime),
                    DialoguesCount = _analyticOfficeUtils.DialoguesCount(dialoguesCur),
                    AvgWorkingTime = _analyticOfficeUtils.SessionAverageHours(sessionCur, begTime, endTime),
                    AvgDurationDialogue = _analyticOfficeUtils.DialogueAverageDuration(dialoguesCur, begTime, endTime),
                    BestEmployee = _analyticOfficeUtils.BestEmployeeLoad(dialoguesCur, sessionCur, begTime, endTime),
                };
                var satisfactionIndex = _analyticOfficeUtils.SatisfactionIndex(dialoguesCur);
                var loadIndex = _analyticOfficeUtils.LoadIndex(sessionCur, dialoguesCur, begTime, endTime.AddDays(1));
                var employeeCount = _analyticOfficeUtils.EmployeeCount(dialoguesCur);
                var deviceCount = _analyticOfficeUtils.DeviceCount(dialoguesCur);
            //   result.CorrelationLoadSatisfaction = satisfactionIndex != 0?  loadIndex / satisfactionIndex : 0;
                result.WorkloadDynamics += result.WorkloadValueAvg;
                result.DialoguesNumberAvgPerEmployee = (dialoguesCur.Count() != 0) ? dialoguesCur.Where( p => p.ApplicationUserId != null).GroupBy(p => p.BegTime.Date).Select(p => p.Count()).Average() / employeeCount : 0;
                result.DialoguesNumberAvgPerDevice = (dialoguesCur.Count() != 0) ? dialoguesCur.GroupBy(p => p.BegTime.Date).Select(p => p.Count()).Average() / employeeCount : 0;
                result.DialoguesNumberAvgPerDayOffice = (dialoguesCur.Count() != 0) ? dialoguesCur.GroupBy(p => p.BegTime.Date).Select(p => p.Count()).Average() : 0;

                var diagramDialogDurationPause = sessionCur
                .GroupBy(p => p.BegTime.Date)
                .Select(p => new
                {
                    Day = p.Key.ToString(),
                    AvgDialogue = _analyticOfficeUtils
                        .DialogueAverageDuration(
                            dialoguesCur.Where(x => x.BegTime >= p.Min(s => s.BegTime) && x.EndTime < p.Max(s => s.EndTime)).ToList(),
                            p.Min(s => s.BegTime),
                            p.Max(s => s.EndTime)),
                    AvgPause = _analyticOfficeUtils
                        .DialogueAveragePause(
                            p.ToList(),
                            dialoguesCur.Where(x => x.BegTime >= p.Min(s => s.BegTime) && x.EndTime < p.Max(s => s.EndTime)).ToList(),
                            p.Min(s => s.BegTime),
                            p.Max(s => s.EndTime)),
                    AvgWorkLoad  = _analyticOfficeUtils
                        .LoadIndex(
                            p.ToList(),
                            dialoguesCur.Where(x => x.BegTime >= p.Min(s => s.BegTime) && x.EndTime < p.Max(s => s.EndTime)).ToList(),
                            p.Min(s => s.BegTime),
                            p.Max(s => s.EndTime))                      
                }).ToList();

                var optimalLoad = 0.7;
                var employeeWorked = sessionCur
                .GroupBy(p => p.BegTime.Date)
                .Select(p => new
                {
                    Day = p.Key.ToString(),
                    EmployeeCount = _analyticOfficeUtils
                        .EmployeeCount(
                            dialoguesCur.Where(x => x.BegTime >= p.Min(s => s.BegTime) && x.EndTime < p.Max(s => s.EndTime)).ToList()
                           ),
                    LoadIndex = _analyticOfficeUtils
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
            var dialogueUserDate = dialoguesCur?
                 .Where(p => p.ApplicationUserId != null)
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


            var dialogueDeviceDate = dialoguesCur?
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

            var pauseInMin = (sessionCur.Count() != 0 && dialoguesCur.Count() != 0) ?
                            _analyticOfficeUtils.DialogueAvgPauseListInMinutes(sessionCur, dialoguesCur, begTime, endTime): null;
                     
                var sessTimeMinutes = _analyticOfficeUtils.SessionTotalHours(sessionCur, begTime, endTime)*60;
                var pausesAmount = new{
                    Less_10 = pauseInMin?.Where(p => p <= 10).Count(),
                    Between_11_20 = pauseInMin?.Where(p => p > 10 && p <= 20).Count(),
                    Between_21_60 = pauseInMin?.Where(p => p > 20 && p <= 60).Count(),
                    More_60 = pauseInMin?.Where(p => p > 60).Count()
                };

                var pausesShareInSession = new{
                    Less_10 = sessTimeMinutes != 0? 100 *  pauseInMin?.Where(p => p <= 10).Sum() / sessTimeMinutes : 0,
                    Between_11_20 = sessTimeMinutes != 0? 100 * pauseInMin?.Where(p => p > 10 && p <= 20).Sum() / sessTimeMinutes : 0,
                    Between_21_60 = sessTimeMinutes != 0? 100 * pauseInMin?.Where(p => p > 20 && p <= 60).Sum() / sessTimeMinutes : 0,
                    More_60 = sessTimeMinutes != 0? 100 * pauseInMin?.Where(p => p > 60).Sum() / sessTimeMinutes : 0,
                    Load = sessTimeMinutes != 0? 100 * (sessTimeMinutes - pauseInMin?.Sum()) / sessTimeMinutes : 0
                };
                 var pausesInMinutes = new{
                    Less_10 = pauseInMin?.Where(p => p <= 10).Sum(),
                    Between_11_20 = pauseInMin?.Where(p => p > 10 && p <= 20).Sum(),
                    Between_21_60 = pauseInMin?.Where(p => p > 20 && p <= 60).Sum() ,
                    More_60 = pauseInMin?.Where(p => p > 60).Sum(),
                    Load = sessTimeMinutes - pauseInMin?.Sum()
                };
  
                var jsonToReturn = new Dictionary<string, object>();
                jsonToReturn["Workload"] = result;
                jsonToReturn["DiagramDialogDurationPause"] = diagramDialogDurationPause;
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
            var sessions = _repository.GetWithInclude<Session>(
                    p => p.BegTime >= prevBeg
                    && p.EndTime <= endTime
                    && p.StatusId == 7
                    && (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                    && (!deviceIds.Any() || deviceIds.Contains(p.DeviceId))
                    , o => o.ApplicationUser)
                .AsQueryable()
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
            List<Guid> deviceIds)
        {
            var dialogues = _repository.GetWithInclude<Dialogue>(
                    p => p.BegTime >= prevBeg
                    && p.EndTime <= endTime
                    && p.StatusId == 3
                    && p.InStatistic == true
                    && (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                    && (!deviceIds.Any() || deviceIds.Contains(p.DeviceId))
                    , p => p.ApplicationUser)
                .AsQueryable()
                .Select(p => new DialogueInfo
                {
                    DialogueId = p.DialogueId,
                    ApplicationUserId = p.ApplicationUserId,
                    DeviceId = p.DeviceId,
                    DeviceName = p.Device.Name,
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