using System;
using System.Collections.Generic;
using System.Linq;
using HBData;
using Microsoft.EntityFrameworkCore;
using UserOperations.Models.Get.AnalyticRatingController;
using HBData.Repository;
using System.Threading.Tasks;
using HBData.Models;
using Newtonsoft.Json;
using UserOperations.Models.AnalyticModels;
using UserOperations.Utils.Interfaces;
using HBLib.Utils.Interfaces;

namespace UserOperations.Services
{
    public class AnalyticRatingService
    {    
        private readonly ILoginService _loginService;
        private readonly IRequestFilters _requestFilters;
        private readonly IAnalyticRatingUtils _analyticRatingUtils;
        private readonly IDBOperations _dbOperations;
        private readonly IGenericRepository _repository;

        public AnalyticRatingService(
            ILoginService loginService,
            IRequestFilters requestFilters,
            IAnalyticRatingUtils analyticRatingUtils,
            IGenericRepository repository,
            IDBOperations dbOperations
            )
        {
            _loginService = loginService;
            _requestFilters = requestFilters;
            _analyticRatingUtils = analyticRatingUtils;
            _repository = repository;
            _dbOperations = dbOperations;
        }

        public async Task<string> RatingProgress( string beg,  string end, 
                                             List<Guid?> applicationUserIds, List<Guid> companyIds, List<Guid> corporationIds, List<Guid> deviceIds)
        {
                var role = _loginService.GetCurrentRoleName();
                var companyId = _loginService.GetCurrentCompanyId();
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);
              //  var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);
                var typeIdCross = await GetCrossPhraseTypeId();

                var sessions = await GetSessions(
                    begTime, endTime,
                    companyIds, applicationUserIds, deviceIds);

                var dialogues = await GetDialogues(
                    begTime, endTime,
                    companyIds, applicationUserIds, deviceIds, typeIdCross);

                var results = dialogues
                    .GroupBy(p => p.ApplicationUserId)
                    .Select(p => new RatingProgressInfo
                    {
                        FullName = p.First().FullName,
                        UserResults = p.GroupBy(q => q.BegTime.Date)
                            .Select(q => new RatingProgressUserInfo
                            {
                                Date = q.Key,
                                DialogueCount = q.Count() != 0 ? q.Select(r => r.DialogueId).Distinct().Count() : 0,
                                TotalScore = q.Count() != 0 ? q.Average(r => r.SatisfactionScore) : null,
                                Load = _analyticRatingUtils.LoadIndex(sessions, q, p.Key, q.Key),
                                LoadHours = _analyticRatingUtils.SessionAverageHours(sessions, p.Key, q.Key),
                                WorkingHours = _analyticRatingUtils.DialogueSumDuration(q),
                                DialogueDuration = _analyticRatingUtils.DialogueAverageDuration(q),
                                CrossInProcents = _analyticRatingUtils.CrossIndex(q)
                            }).ToList()
                    }).ToList();
                return JsonConvert.SerializeObject(results);
            }


        public async Task<string> RatingUsers( string beg, string end, 
                                          List<Guid?> applicationUserIds, List<Guid> companyIds, List<Guid> corporationIds, List<Guid> deviceIds)
        {
                var role = _loginService.GetCurrentRoleName();
                var companyId = _loginService.GetCurrentCompanyId();
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);       
               // var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);

                var sessions = await GetSessions(
                    begTime, endTime,
                    companyIds, applicationUserIds, deviceIds);

                var typeIdCross = await GetCrossPhraseTypeId();

                var dialogues = await GetDialogues(
                    begTime, endTime,
                    companyIds, applicationUserIds, deviceIds, typeIdCross
                );

                var active = 3;
                var workingTimes = _repository.GetAsQueryable<WorkingTime>()
                    .Where(x => !companyIds.Any() || companyIds.Contains(x.CompanyId)).ToArray();
                var devicesFiltered = _repository.GetAsQueryable<Device>()
                    .Where(x => companyIds.Contains(x.CompanyId)
                        && (!deviceIds.Any() || deviceIds.Contains(x.DeviceId))
                        && x.StatusId == active)
                    .ToList();
                var timeTableForDevices = _dbOperations.WorkingTimeDoubleListForOneUserInCompanys(workingTimes, begTime, endTime, companyIds, devicesFiltered, role);
                var result = dialogues
                    .GroupBy(p => p.ApplicationUserId, (Key, group) => new
                        {
                            ApplicationUserId = Key,
                            DialoguesInfos = group
                        })
                    .Select(p => new RatingUserInfo
                    {
                        FullName = p.DialoguesInfos.First().FullName,
                        SatisfactionIndex = _analyticRatingUtils.SatisfactionIndex(p.DialoguesInfos.ToList()),
                        LoadIndex = _analyticRatingUtils.LoadIndexWithTimeTableForUser(timeTableForDevices, p.DialoguesInfos.ToList(), begTime, endTime),
                        CrossIndex = _analyticRatingUtils.CrossIndex(p.DialoguesInfos.ToList()),
                        DialoguesCount = p.DialoguesInfos.Select(q => q.DialogueId).Distinct().Count(),
                        CompanyId = p.DialoguesInfos.First().CompanyId.ToString()
                    }).ToList();
                var emptyUsers = sessions.GroupBy(p => p.ApplicationUserId)
                    .Where(p => !result.Select(x=>x.FullName).Contains(p.First().FullName))
                    .Select(p => new RatingUserInfo
                    {
                        FullName = p.First().FullName,
                        SatisfactionIndex = 0,
                        LoadIndex = 0,
                        CrossIndex = 0,
                        DialoguesCount = 0,
                        CompanyId = p.First().CompanyId.ToString()
                    }).ToList();

                result = result.Union(emptyUsers).OrderByDescending(p => p.SatisfactionIndex).ToList();
                return JsonConvert.SerializeObject(result);            
        }  


        public async Task<string> RatingOffices( 
                            string beg, string end, 
                            List<Guid> companyIds, 
                            List<Guid> corporationIds)
        {
                int active = 3;
                var role = _loginService.GetCurrentRoleName();
                var companyId = _loginService.GetCurrentCompanyId();
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);
                //var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);

                var workingTimes = _repository.GetAsQueryable<WorkingTime>().Where(x => !companyIds.Any() || companyIds.Contains(x.CompanyId)).ToArray();
                var typeIdCross = await GetCrossPhraseTypeId();

                var dialogues = await GetDialogueInfoCompanys(
                    begTime, endTime,
                    companyIds, typeIdCross, workingTimes
                );


                var result = dialogues
                    .GroupBy(p => p.CompanyId)
                    .Select(p => new RatingOfficeInfo
                    {
                        CompanyId = p.Key.ToString(),
                        FullName = p.First().FullName,
                        SatisfactionIndex = _analyticRatingUtils.SatisfactionIndex(p),
                        LoadIndex = _dbOperations.WorklLoadByTimeIndex(
                                _dbOperations.WorkingTimeDoubleListInMinForOneCompany(
                                                workingTimes, 
                                                begTime, endTime, 
                                                (Guid)p.Key, 
                                                p.Select(x => x.DeviceId).Count()), 
                                p.Where(x => x.CompanyId == p.Key && x.IsInWorkingTime).ToList(), 
                                begTime, endTime),
                        CrossIndex = _analyticRatingUtils.CrossIndex(p),
                        Recommendation = "",
                        DialoguesCount = p.Select(q => q.DialogueId).Distinct().Count(),
                        DaysCount = p.Select(q => q.BegTime.Date).Distinct().Count(),
                        WorkingHoursDaily = _analyticRatingUtils.DialogueAverageDuration(p, begTime, endTime),
                        DialogueAverageDuration = _analyticRatingUtils.DialogueAverageDuration(p, begTime, endTime),
                        DialogueAveragePause = _analyticRatingUtils.DialogueHourAveragePause(
                                 _dbOperations.WorkingTimeDoubleListInMinForOneCompany(
                                                workingTimes,
                                                begTime, endTime,
                                                (Guid)p.Key,
                                                p.Select(x => x.DeviceId).Count()), 
                                 p.Where(x => x.CompanyId == p.Key && x.IsInWorkingTime).ToList(), 
                                 begTime, endTime)
                    }).ToList();
                result = result.OrderBy(p => p.EfficiencyIndex).ToList();
                return JsonConvert.SerializeObject(result);
        }


        //---PRIVATE---
        private async Task<Guid> GetCrossPhraseTypeId()
        {
            var typeIdCross = await _repository.GetAsQueryable<PhraseType>()
                    .Where(p => p.PhraseTypeText == "Cross")
                    .Select(p => p.PhraseTypeId).FirstOrDefaultAsync();
            return typeIdCross;
        }

        private async Task<List<SessionInfo>> GetSessions(
            DateTime begTime, DateTime endTime,
            List<Guid> companyIds,
            List<Guid?> applicationUserIds,
            List<Guid> deviceIds)
        {
            var sessions = await _repository.GetAsQueryable<Session>()
                .Where(p => p.BegTime >= begTime
                    && p.EndTime <= endTime
                    && p.StatusId == 7
                    && (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                    && (!deviceIds.Any() || deviceIds.Contains(p.DeviceId))
                    && p.ApplicationUserId != null)
                .Select(p => new SessionInfo
                {
                    ApplicationUserId = p.ApplicationUserId,
                    DeviceId = p.DeviceId,
                    BegTime = p.BegTime,
                    EndTime = p.EndTime,
                    FullName = p.ApplicationUser != null? p.ApplicationUser.FullName : null,
                    CompanyId = p.Device.CompanyId
                })
                .ToListAsync();
            return sessions;
        }

        private async Task<List<DialogueInfo>> GetDialogues(
            DateTime begTime, DateTime endTime,
            List<Guid> companyIds,
            List<Guid?> applicationUserIds,
            List<Guid> deviceIds,
            Guid typeIdCross)
        {
            var dialogues = await _repository.GetAsQueryable<Dialogue>()
                .Where(p => p.BegTime >= begTime
                    && p.EndTime <= endTime
                    && p.StatusId == 3
                    && p.InStatistic == true
                    && (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                    && (!deviceIds.Any() || deviceIds.Contains(p.DeviceId))
                    && p.ApplicationUserId != null)
                .Select(p => new DialogueInfo
                {
                    DialogueId = p.DialogueId,
                    CompanyId = p.Device.CompanyId,
                    DeviceId = p.DeviceId,
                    ApplicationUserId = p.ApplicationUserId,
                    BegTime = p.BegTime,
                    EndTime = p.EndTime,
                    SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                    FullName = p.ApplicationUser != null? p.ApplicationUser.FullName : null,
                    CrossCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdCross).Count()
                })
                .ToListAsync();
            return dialogues;
        }

        private async Task<List<SessionInfo>> GetSessionInfoCompanys(
            DateTime begTime, DateTime endTime,
            List<Guid> companyIds,
            List<Guid> deviceIds)
        {
            var sessions = await _repository.GetAsQueryable<Session>()
                .Where(p => p.BegTime >= begTime
                    && p.EndTime <= endTime
                    && p.StatusId == 7
                    && (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId))
                    && (!deviceIds.Any() || deviceIds.Contains(p.DeviceId)))
                .Select(p => new SessionInfo
                {
                    CompanyId = p.Device.CompanyId,
                    DeviceId = p.DeviceId,
                    BegTime = p.BegTime,
                    EndTime = p.EndTime
                })
                .ToListAsync();
            return sessions;
        }

        private async Task<List<DialogueInfo>> GetDialogueInfoCompanys(
            DateTime begTime, DateTime endTime,
            List<Guid> companyIds,
            Guid typeIdCross,
            WorkingTime [] workingTimes)
        {
            var dialogues = await _repository.GetAsQueryable<Dialogue>()
                .Where(p => p.BegTime >= begTime
                    && p.EndTime <= endTime
                    && p.StatusId == 3
                    && p.InStatistic == true
                    && (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId)))
                .Select(p => new DialogueInfo
                {
                    DialogueId = p.DialogueId,
                    DeviceId = p.DeviceId,
                    CompanyId = p.Device.CompanyId,
                    BegTime = p.BegTime,
                    EndTime = p.EndTime,
                    SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                    FullName = p.Device.Company.CompanyName,
                    CrossCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdCross).Count(),
                    IsInWorkingTime = _dbOperations.CheckIfDialogueInWorkingTime(p, workingTimes.Where(x => x.CompanyId == p.Device.CompanyId).ToArray())
                })
                .ToListAsync();
            return dialogues;
        }
    }
}