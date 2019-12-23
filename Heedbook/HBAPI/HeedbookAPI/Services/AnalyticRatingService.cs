using System;
using System.Collections.Generic;
using System.Linq;
using HBData;
using Microsoft.EntityFrameworkCore;
using UserOperations.Utils;
using UserOperations.Models.Get.AnalyticRatingController;
using UserOperations.Utils.AnalyticRatingUtils;
using HBData.Repository;
using System.Threading.Tasks;
using HBData.Models;
using Newtonsoft.Json;

namespace UserOperations.Services
{
    public class AnalyticRatingService
    {    
        private readonly LoginService _loginService;
        private readonly RecordsContext _context;
        private readonly RequestFilters _requestFilters;
        private readonly AnalyticRatingUtils _analyticRatingUtils;
        private readonly IGenericRepository _repository;

        public AnalyticRatingService(
            LoginService loginService,
            RecordsContext context,
            RequestFilters requestFilters,
            AnalyticRatingUtils analyticRatingUtils,
            IGenericRepository repository
            )
        {
            _loginService = loginService;
            _context = context;
            _requestFilters = requestFilters;
            _analyticRatingUtils = analyticRatingUtils;
            _repository = repository;
        }


        public async Task<string> RatingProgress( string beg,  string end, 
                                             List<Guid> applicationUserIds, List<Guid> companyIds, List<Guid> corporationIds, List<Guid> workerTypeIds)
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
                    companyIds, applicationUserIds, workerTypeIds);

                var dialogues = await GetDialogues(
                    begTime, endTime,
                    companyIds, applicationUserIds, workerTypeIds, typeIdCross);

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
                                CrossInProcents = _analyticRatingUtils.CrossIndex(p)
                            }).ToList()
                    }).ToList();
                return JsonConvert.SerializeObject(results);
            }


        public async Task<List<RatingUserInfo>> RatingUsers( string beg, string end, 
                                          List<Guid> applicationUserIds, List<Guid> companyIds, List<Guid> corporationIds, List<Guid> workerTypeIds )
        {
                var role = _loginService.GetCurrentRoleName();
                var companyId = _loginService.GetCurrentCompanyId();
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);       
               // var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);

                var sessions = await GetSessions(
                    begTime, endTime,
                    companyIds, applicationUserIds, workerTypeIds);

                var typeIdCross = await GetCrossPhraseTypeId();

                var dialogues = await GetDialogues(
                    begTime, endTime,
                    companyIds, applicationUserIds, workerTypeIds, typeIdCross
                );

                var result = dialogues
                    .GroupBy(p => p.ApplicationUserId)
                    .Select(p => new RatingUserInfo
                    {
                        FullName = p.First().FullName,
                        SatisfactionIndex = _analyticRatingUtils.SatisfactionIndex(p),
                        LoadIndex = _analyticRatingUtils.LoadIndex(sessions, p, begTime, endTime),
                        CrossIndex = _analyticRatingUtils.CrossIndex(p),
                        DialoguesCount = p.Select(q => q.DialogueId).Distinct().Count(),
                        CompanyId = p.First().CompanyId.ToString()
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
                return result;
        }  


        public async Task<List<RatingOfficeInfo>> RatingOffices( string beg, string end, 
                                                                 List<Guid> companyIds, List<Guid> corporationIds, List<Guid> workerTypeIds )
        {
                var role = _loginService.GetCurrentRoleName();
                var companyId = _loginService.GetCurrentCompanyId();
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);       
                //var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);

                var sessions = await GetSessionInfoCompanys(
                    begTime,
                    endTime,
                    companyIds,
                    workerTypeIds
                );

                var typeIdCross = await GetCrossPhraseTypeId();

                var dialogues = await GetDialogueInfoCompanys(
                    begTime, endTime,
                    companyIds, workerTypeIds, typeIdCross
                );

                var result = dialogues
                    .GroupBy(p => p.CompanyId)
                    .Select(p => new RatingOfficeInfo
                    {
                        CompanyId = p.Key.ToString(),
                        FullName = p.First().FullName,
                        SatisfactionIndex = _analyticRatingUtils.SatisfactionIndex(p),
                        LoadIndex = _analyticRatingUtils.LoadIndex(sessions, p, begTime, endTime),
                        CrossIndex = _analyticRatingUtils.CrossIndex(p),
                        Recommendation = "",
                        DialoguesCount = p.Select(q => q.DialogueId).Distinct().Count(),
                        DaysCount = p.Select(q => q.BegTime.Date).Distinct().Count(),
                        WorkingHoursDaily = _analyticRatingUtils.DialogueAverageDuration(p, begTime, endTime),
                        DialogueAverageDuration = _analyticRatingUtils.DialogueAverageDuration(p, begTime, endTime),
                        DialogueAveragePause = _analyticRatingUtils.DialogueAveragePause(sessions, p, begTime, endTime)
                    }).ToList();
                result = result.OrderBy(p => p.EfficiencyIndex).ToList();
                return result;
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
            List<Guid> applicationUserIds,
            List<Guid> workerTypeIds)
        {
            var sessions = await _repository.GetAsQueryable<Session>()
                .Where(p => p.BegTime >= begTime
                    && p.EndTime <= endTime
                    && p.StatusId == 7
                    && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                    && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)))
                .Select(p => new SessionInfo
                {
                    ApplicationUserId = p.ApplicationUserId,
                    BegTime = p.BegTime,
                    EndTime = p.EndTime,
                    FullName = p.ApplicationUser.FullName,
                    CompanyId = p.ApplicationUser.CompanyId
                })
                .ToListAsync();
            return sessions;
        }
        private async Task<List<DialogueInfo>> GetDialogues(
            DateTime begTime, DateTime endTime,
            List<Guid> companyIds,
            List<Guid> applicationUserIds,
            List<Guid> workerTypeIds,
            Guid typeIdCross)
        {
            var dialogues = await _repository.GetAsQueryable<Dialogue>()
                .Where(p => p.BegTime >= begTime
                    && p.EndTime <= endTime
                    && p.StatusId == 3
                    && p.InStatistic == true
                    && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                    && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)))
                .Select(p => new DialogueInfo
                {
                    DialogueId = p.DialogueId,
                    CompanyId = p.ApplicationUser.CompanyId,
                    ApplicationUserId = p.ApplicationUserId,
                    BegTime = p.BegTime,
                    EndTime = p.EndTime,
                    SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                    FullName = p.ApplicationUser.FullName,
                    CrossCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdCross).Count()
                })
                .ToListAsync();
            return dialogues;
        }
        private async Task<List<SessionInfoCompany>> GetSessionInfoCompanys(
            DateTime begTime, DateTime endTime,
            List<Guid> companyIds,
            List<Guid> workerTypeIds)
        {
            var sessions = await _repository.GetAsQueryable<Session>()
                .Where(p => p.BegTime >= begTime
                    && p.EndTime <= endTime
                    && p.StatusId == 7
                    && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                    && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)))
                .Select(p => new SessionInfoCompany
                {
                    CompanyId = (Guid)p.ApplicationUser.CompanyId,
                    BegTime = p.BegTime,
                    EndTime = p.EndTime
                })
                .ToListAsync();
            return sessions;
        }
        private async Task<List<DialogueInfoCompany>> GetDialogueInfoCompanys(
            DateTime begTime, DateTime endTime,
            List<Guid> companyIds,
            List<Guid> workerTypeIds,
            Guid typeIdCross)
        {
            var dialogues = await _repository.GetAsQueryable<Dialogue>()
                .Where(p => p.BegTime >= begTime
                    && p.EndTime <= endTime
                    && p.StatusId == 3
                    && p.InStatistic == true
                    && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                    && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)))
                .Select(p => new DialogueInfoCompany
                {
                    DialogueId = p.DialogueId,
                    CompanyId = (Guid)p.ApplicationUser.CompanyId,
                    BegTime = p.BegTime,
                    EndTime = p.EndTime,
                    SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                    FullName = p.ApplicationUser.Company.CompanyName,
                    CrossCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdCross).Count()
                })
                .ToListAsync();
            return dialogues;
        }
    }
}