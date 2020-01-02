using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using UserOperations.Models.Get.AnalyticReportController;
using Newtonsoft.Json;
using UserOperations.Utils.AnalyticReportUtils;
using UserOperations.Utils;
using HBData.Repository;
using HBData.Models;
using UserOperations.Models.AnalyticModels;

namespace UserOperations.Services
{
    public class AnalyticReportService
    {
        private readonly IConfiguration _config;
        private readonly LoginService _loginService;
        private readonly RequestFilters _requestFilters;
        private readonly IGenericRepository _repository;
        private readonly AnalyticReportUtils _analyticReportUtils;

        public AnalyticReportService(
            IConfiguration config,
            LoginService loginService,
            RequestFilters requestFilters,
            IGenericRepository repository,
            AnalyticReportUtils analyticReportUtils
            )
        {
            _config = config;
            _loginService = loginService;
            _requestFilters = requestFilters;
            _repository = repository;
            _analyticReportUtils = analyticReportUtils;
        }

        public string ReportActiveEmployee( List<Guid?> applicationUserIds, List<Guid> companyIds, 
                                                   List<Guid> corporationIds, List<Guid> deviceIds)
        {
                var role = _loginService.GetCurrentRoleName();
                var companyId = _loginService.GetCurrentCompanyId();
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);
                var sessions = GetSessions(companyIds, applicationUserIds, deviceIds);
                return JsonConvert.SerializeObject(sessions);
        }

        public string ReportUserPartial( string beg, string end,
                                                  List<Guid?> applicationUserIds, List<Guid> companyIds,
                                               List<Guid> corporationIds, List<Guid> deviceIds)
        {
                var role = _loginService.GetCurrentRoleName();
                var companyId = _loginService.GetCurrentCompanyId();
                var currentUserId = _loginService.GetCurrentUserId();
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);

                var employeeRole = GetEmployeeRoleId();
                var sessions = GetSessionsWithTimeFilter(
                    begTime,
                    endTime,
                    employeeRole,
                    companyIds,
                    applicationUserIds,
                    deviceIds);
                var dialogues = GetDialogues(
                    begTime, 
                    endTime, 
                    companyIds, 
                    applicationUserIds,
                    deviceIds);
                    //---USER WITHOUT SESSIONS---
                var userIds = sessions.Select(x => x.ApplicationUserId).Distinct().ToList();
                var usersToAdd = GetApplicationUsersToAdd(
                    endTime,
                    companyIds,
                    applicationUserIds,
                    userIds,
                    currentUserId,
                    employeeRole);
                var result = sessions
                    .GroupBy(p => p.ApplicationUserId)
                    .Select(p => new 
                    {
                        p.First().FullName,
                        ApplicationUserId = p.Key,
                        LoadIndexAverage = _analyticReportUtils.LoadIndex(p, dialogues, begTime, endTime),
                        PeriodInfo = p.GroupBy(q => q.BegTime.Date).Select(q => new ReportPartDayEmployeeInfo
                        {
                            Date = q.Key,
                            WorkingHours = _analyticReportUtils.Min(24, _analyticReportUtils.MaxDouble(_analyticReportUtils.SessionAverageHours(q), _analyticReportUtils.DialogueSumDuration(q, dialogues, p.Key))?? 0),
                            DialogueHours = _analyticReportUtils.Min(24, _analyticReportUtils.DialogueSumDuration(q, dialogues, p.Key)?? 0),
                            LoadIndex = 100 * _analyticReportUtils.LoadIndex(_analyticReportUtils.SessionAverageHours(q), _analyticReportUtils.DialogueSumDuration(q, dialogues, p.Key)),
                            DialogueCount = _analyticReportUtils.DialoguesCount(dialogues, p.Key, q.Key)
                        }).ToList()
                    }).ToList();

                var emptyUsers = usersToAdd.Select(p => new 
                    {
                        p.FullName,
                        ApplicationUserId = (Guid?)p.Id,
                        LoadIndexAverage = (double?)0,
                        PeriodInfo =  new List<ReportPartDayEmployeeInfo>()
                        }).ToList();
                   
                result.AddRange(emptyUsers);
                return JsonConvert.SerializeObject(result);

            }

        public string ReportUserFull( string beg, string end,
                                         List<Guid?> applicationUserIds, List<Guid> companyIds, List<Guid> corporationIds, List<Guid> deviceIds)
        {
            var role = _loginService.GetCurrentRoleName();
            var companyId = _loginService.GetCurrentCompanyId();
            var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);    
                var employeeRole = GetEmployeeRoleId();
                var sessions = GetSessionsWithTimeFilter(
                    begTime,
                    endTime,
                    employeeRole,
                    companyIds,
                    applicationUserIds,
                    deviceIds);
                var dialogues = GetDialoguesWithWorkerType(
                    begTime,
                    endTime,
                    employeeRole,
                    companyIds,
                    applicationUserIds,
                    deviceIds
                );

                var result = new List<ReportFullPeriodInfo>();

                foreach (var date in dialogues.Select(p => p.BegTime.Date).Distinct().ToList())
                {
                    foreach (var applicationUserId in dialogues.Select(p => p.ApplicationUserId).Distinct().ToList())
                    {

                        var userInfo = new ReportFullPeriodInfo();
                        var dialoguesUser = dialogues.Where(p => p.ApplicationUserId == applicationUserId && p.BegTime.Date == date).ToList();
                        if (dialoguesUser.Any())
                        {                          
                            var begDate = Convert.ToDateTime(date);
                            var endDate = Convert.ToDateTime(date).AddDays(1);
                            userInfo.ApplicationUserId = applicationUserId;
                            userInfo.Date = Convert.ToDateTime(date);
                            userInfo.FullName = dialoguesUser.FirstOrDefault().FullName;
                            userInfo.Load = _analyticReportUtils.LoadIndex(sessions, dialoguesUser, applicationUserId, Convert.ToDateTime(date), begDate, endDate);
                            userInfo.DialoguesTime = _analyticReportUtils.DialogueSumDuration(dialoguesUser, begDate, endDate);
                            userInfo.SessionTime = _analyticReportUtils.SessionAverageHours(sessions, applicationUserId, Convert.ToDateTime(date), begDate, endDate);
                            userInfo.PeriodInfo = _analyticReportUtils.TimeTable(sessions, dialoguesUser, applicationUserId, Convert.ToDateTime(date));

                            result.Add(userInfo);
                        }
                    }
                }
                return JsonConvert.SerializeObject(result);
        }


        //---PRIVATE---
        private List<SessionInfo> GetSessions(
          List<Guid> companyIds,
          List<Guid?> applicationUserIds,
          List<Guid> deviceIds)
        {
            var sessions = _repository.GetAsQueryable<Session>()
                .Where(p =>
                    p.StatusId == 6
                    && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                    && (!deviceIds.Any() || deviceIds.Contains(p.DeviceId)))
                .Select(p => new SessionInfo
                {
                    ApplicationUserId = p.ApplicationUserId,
                    FullName = p.ApplicationUser.FullName
                })
                .ToList().Distinct().ToList();
            return sessions;
        }

        private List<SessionInfo> GetSessionsWithTimeFilter(
            DateTime begTime,
            DateTime endTime,
            Guid employeeRole,
            List<Guid> companyIds,
            List<Guid?> applicationUserIds,
            List<Guid> deviceIds)
        {
            var sessions = _repository.GetAsQueryable<Session>()
                .Where(p =>
                    p.BegTime >= begTime
                    && p.EndTime <= endTime
                    && p.StatusId == 7
                    && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                    && (!deviceIds.Any() || deviceIds.Contains(p.DeviceId))
                    && (p.ApplicationUser.UserRoles.Any(x => x.RoleId == employeeRole)))
                .Select(p => new SessionInfo
                {
                    ApplicationUserId = p.ApplicationUserId,
                    BegTime = p.BegTime,
                    EndTime = p.EndTime,
                    FullName = p.ApplicationUser.FullName,
                    // WorkerType = p.ApplicationUser.WorkerType.WorkerTypeName,
                })
                .ToList();
            return sessions;
        }
        private Guid GetEmployeeRoleId()
        {
            var roleId = _repository.GetAsQueryable<ApplicationRole>().FirstOrDefault(x => x.Name == "Employee").Id;
            return roleId;
        }

        private List<DialogueInfo> GetDialogues(
            DateTime begTime,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid?> applicationUserIds,
            List<Guid> deviceIds)
        {
            var dialogues = _repository.GetAsQueryable<Dialogue>()
                .Where(p => p.BegTime >= begTime
                    && p.EndTime <= endTime
                    && p.StatusId == 3
                    && p.InStatistic == true
                    && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                    && (!deviceIds.Any() || deviceIds.Contains(p.DeviceId)))
                .Select(p => new DialogueInfo
                {
                    DialogueId = p.DialogueId,
                    ApplicationUserId = p.ApplicationUserId,
                    DeviceId = p.DeviceId,
                    FullName = p.ApplicationUser.FullName,
                    BegTime = p.BegTime,
                    EndTime = p.EndTime,
                    SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal
                })
                .ToList();
            return dialogues;
        }
        private List<DialogueInfo> GetDialoguesWithWorkerType(
            DateTime begTime,
            DateTime endTime,
            Guid employeeRole,
            List<Guid> companyIds,
            List<Guid?> applicationUserIds,
            List<Guid> deviceIds)
        {
            var dialogues = _repository.GetAsQueryable<Dialogue>()
                .Where(p => p.BegTime >= begTime
                        && p.EndTime <= endTime
                        && p.StatusId == 3
                        && p.InStatistic == true
                        && (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId))
                        && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                        && (!deviceIds.Any() || deviceIds.Contains(p.DeviceId))
                        && (p.ApplicationUser.UserRoles.Any(x => x.RoleId == employeeRole)))
                .Select(p => new DialogueInfo
                {
                    DialogueId = p.DialogueId,
                    ApplicationUserId = p.ApplicationUserId,
                    DeviceId = p.DeviceId,
                    FullName = p.ApplicationUser.FullName,
                    BegTime = p.BegTime,
                    EndTime = p.EndTime,
                    SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                    // Date = DbFunctions.TruncateTime(p.BegTime)
                })
                .ToList();
            return dialogues;
        }
        private List<ApplicationUser> GetApplicationUsersToAdd(
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid?> applicationUserIds,
            List<Guid?> userIds,
            Guid currentUserId,
            Guid employeeRole )
        {
            var users = _repository.GetAsQueryable<ApplicationUser>()
                .Where(p =>
                    p.CreationDate <= endTime
                    && p.StatusId == 3
                    && (!companyIds.Any() || companyIds.Contains((Guid)p.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.Id))
                    && !userIds.Contains(p.Id)
                    && p.Id != currentUserId
                    && (p.UserRoles.Any(x => x.RoleId == employeeRole)))
                .ToList();

            return users;
        }
    }
}