using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using UserOperations.Utils;
using HBData.Models;
using UserOperations.Utils.AnalyticWeeklyReportController;
using HBData.Repository;
using UserOperations.Models.AnalyticModels;

namespace UserOperations.Services
{
    public class AnalyticWeeklyReportService
    {
        private readonly LoginService _loginService;
        private readonly RequestFilters _requestFilters;
        private readonly IGenericRepository _repository;
        private readonly AnalyticWeeklyReportUtils _analyticWeeklyReportUtils;

        public AnalyticWeeklyReportService(
            LoginService loginService,
            RequestFilters requestFilters,
            IGenericRepository repository,
            AnalyticWeeklyReportUtils analyticWeeklyReportUtils
            )
        {
            _loginService = loginService;
            _requestFilters = requestFilters;
            _repository = repository;
            _analyticWeeklyReportUtils = analyticWeeklyReportUtils;
        }

        public Dictionary<string, object> User( Guid userId )
        { 
                var begTime = DateTime.Now.AddDays(-7);
                var employeeRoleId = GetEmployeeRoleId();
                var jsonToReturn = new Dictionary<string, object>();
                var companyId = GetCompanyId(userId);
                var corporationId = GetCorporationId(companyId);
                var userIdsInCorporation = corporationId != null
                    ? GetUserIdsInCorporation(corporationId, employeeRoleId) 
                    : null;
                var userIdsInCompany = GetUserIdsInCompany(companyId, employeeRoleId);

                //----ALL FOR WEEK Corporation---
                var sessionsCorporation = userIdsInCorporation != null
                    ? GetSessionMoreThanBegTime(userIdsInCorporation, begTime) 
                    : null;
                var sessionsCorporationOld = userIdsInCorporation != null
                    ? GetSessionLessThanBegTime(userIdsInCorporation, begTime) 
                    : null;
                var dialoguesCorporation = userIdsInCorporation != null
                    ? GetDialoguesMoreThanBegTime(userIdsInCorporation, begTime) 
                    : null;
                var dialoguesCorporationOld = userIdsInCorporation != null
                    ? GetDialoguesLessThanBegTime(userIdsInCorporation, begTime)
                    : null;
                //----ALL FOR WEEK Company---
                var sessionsCompany = GetSessionMoreThanBegTime(userIdsInCompany, begTime);
                var sessionsCompanyOld = GetSessionLessThanBegTime(userIdsInCompany, begTime);
                var dialoguesCompany = GetDialoguesMoreThanBegTime(userIdsInCompany, begTime);
                var dialoguesCompanyOld = GetDialoguesLessThanBegTime(userIdsInCompany, begTime);
                //-----for User---
                var userSessions = sessionsCompany.Where(p => p.AspNetUserId == userId).ToList();
                var userSessionsOld = sessionsCompanyOld.Where(p => p.AspNetUserId == userId).ToList();
                var userDialogues = dialoguesCompany.Where(p => p.AspNetUserId == userId).ToList();
                var userDialoguesOld = dialoguesCompanyOld.Where(p => p.AspNetUserId == userId).ToList();

                var usersInCorporation = userIdsInCorporation != null? userIdsInCorporation.Count() : 0;
                var usersInCompany = userIdsInCompany.Count();

                //----satisfaction--------
                var Satisfaction = new UserWeeklyInfo(usersInCorporation, usersInCompany)
                {
                    TotalAvg = _analyticWeeklyReportUtils.TotalAvg(userDialogues, "Satisfaction"),
                    AvgPerDay = _analyticWeeklyReportUtils.AvgPerDay(userDialogues, "Satisfaction"),
                    OfficeRating = _analyticWeeklyReportUtils.OfficeRatingSatisfactionPlace(dialoguesCompany, userId),
                    CorporationRating = _analyticWeeklyReportUtils.OfficeRatingSatisfactionPlace(dialoguesCorporation, userId)
                };
                var TotalAvgOld = _analyticWeeklyReportUtils.TotalAvg(userDialoguesOld, "Satisfaction");
                int? OfficeRatingOld = _analyticWeeklyReportUtils.OfficeRatingSatisfactionPlace(dialoguesCompanyOld, userId);
                Satisfaction.Dynamic = Satisfaction.TotalAvg - TotalAvgOld;
                Satisfaction.OfficeRatingChanges = Satisfaction.OfficeRating - OfficeRatingOld;

                int? CorporationRatingOld = _analyticWeeklyReportUtils.OfficeRatingSatisfactionPlace(dialoguesCorporationOld, userId);
                Satisfaction.CorporationRatingChanges = Satisfaction.CorporationRating - CorporationRatingOld;
                jsonToReturn["Satisfaction"] = Satisfaction;

                //   //----positiveEmotions--------
                var PositiveEmotions = new UserWeeklyInfo(usersInCorporation, usersInCompany)
                {
                    TotalAvg = _analyticWeeklyReportUtils.TotalAvg(userDialogues, "PositiveEmotions"),
                    AvgPerDay = _analyticWeeklyReportUtils.AvgPerDay(userDialogues, "PositiveEmotions"),
                    OfficeRating = _analyticWeeklyReportUtils.OfficeRatingPositiveEmotPlace(dialoguesCompany, userId),
                    CorporationRating = _analyticWeeklyReportUtils.OfficeRatingPositiveEmotPlace(dialoguesCorporation, userId)
                };
                TotalAvgOld = _analyticWeeklyReportUtils.TotalAvg(userDialoguesOld, "PositiveEmotions");
                OfficeRatingOld = _analyticWeeklyReportUtils.OfficeRatingPositiveEmotPlace(dialoguesCompanyOld, userId);
                PositiveEmotions.Dynamic = PositiveEmotions.TotalAvg - TotalAvgOld;
                PositiveEmotions.OfficeRatingChanges = PositiveEmotions.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _analyticWeeklyReportUtils.OfficeRatingPositiveEmotPlace(dialoguesCorporationOld, userId);
                PositiveEmotions.CorporationRatingChanges = PositiveEmotions.CorporationRating - CorporationRatingOld;
                jsonToReturn["PositiveEmotions"] = PositiveEmotions;

                //    //----positiveIntonations--------
                var PositiveIntonations = new UserWeeklyInfo(usersInCorporation, usersInCompany)
                {
                    TotalAvg = _analyticWeeklyReportUtils.TotalAvg(userDialogues, "PositiveTone"),
                    AvgPerDay = _analyticWeeklyReportUtils.AvgPerDay(userDialogues, "PositiveTone"),
                    OfficeRating = _analyticWeeklyReportUtils.OfficeRatingPositiveIntonationPlace(dialoguesCompany, userId),
                    CorporationRating = _analyticWeeklyReportUtils.OfficeRatingPositiveIntonationPlace(dialoguesCorporation, userId)
                };
                TotalAvgOld = _analyticWeeklyReportUtils.TotalAvg(userDialoguesOld, "PositiveTone");
                OfficeRatingOld = _analyticWeeklyReportUtils.OfficeRatingPositiveIntonationPlace(dialoguesCompanyOld, userId);
                PositiveIntonations.Dynamic = PositiveIntonations.TotalAvg - TotalAvgOld;
                PositiveIntonations.OfficeRatingChanges = PositiveIntonations.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _analyticWeeklyReportUtils.OfficeRatingPositiveIntonationPlace(dialoguesCorporationOld, userId);
                PositiveIntonations.CorporationRatingChanges = PositiveIntonations.CorporationRating - CorporationRatingOld;
                jsonToReturn["PositiveIntonations"] = PositiveIntonations;

                //    //----speechEmotivity--------
                var SpeechEmotivity = new UserWeeklyInfo(usersInCorporation, usersInCompany)
                {
                    TotalAvg = _analyticWeeklyReportUtils.TotalAvg(userDialogues, "SpeekEmotions"),
                    AvgPerDay = _analyticWeeklyReportUtils.AvgPerDay(userDialogues, "SpeekEmotions"),
                    OfficeRating = _analyticWeeklyReportUtils.OfficeRatingSpeechEmotPlace(dialoguesCompany, userId),
                    CorporationRating = _analyticWeeklyReportUtils.OfficeRatingSpeechEmotPlace(dialoguesCorporation, userId)
                };
                TotalAvgOld = _analyticWeeklyReportUtils.TotalAvg(userDialoguesOld, "SpeekEmotions");
                OfficeRatingOld = _analyticWeeklyReportUtils.OfficeRatingSpeechEmotPlace(dialoguesCompanyOld, userId);
                SpeechEmotivity.Dynamic = SpeechEmotivity.TotalAvg - TotalAvgOld;
                SpeechEmotivity.OfficeRatingChanges = SpeechEmotivity.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _analyticWeeklyReportUtils.OfficeRatingSpeechEmotPlace(dialoguesCorporationOld, userId);
                SpeechEmotivity.CorporationRatingChanges = SpeechEmotivity.CorporationRating - CorporationRatingOld;
                jsonToReturn["SpeechEmotivity"] = SpeechEmotivity;

                //    //----workload--------
                //---number of dialogues per day--
                var NumberOfDialogues = new UserWeeklyInfo(usersInCorporation, usersInCompany)
                {
                    TotalAvg = userDialogues.Sum(p => p.Dialogues),
                    AvgPerDay = _analyticWeeklyReportUtils.AvgNumberOfDialoguesPerDay(userDialogues),
                    OfficeRating = _analyticWeeklyReportUtils.OfficeRatingDialoguesAmount(dialoguesCompany, userId),
                    CorporationRating = _analyticWeeklyReportUtils.OfficeRatingDialoguesAmount(dialoguesCorporation, userId)
                };
                TotalAvgOld = userDialoguesOld.Sum(p => p.Dialogues);
                OfficeRatingOld = _analyticWeeklyReportUtils.OfficeRatingDialoguesAmount(dialoguesCompanyOld, userId);
                NumberOfDialogues.Dynamic = NumberOfDialogues.TotalAvg - TotalAvgOld;
                NumberOfDialogues.OfficeRatingChanges = NumberOfDialogues.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _analyticWeeklyReportUtils.OfficeRatingDialoguesAmount(dialoguesCorporationOld, userId);
                NumberOfDialogues.CorporationRatingChanges = NumberOfDialogues.CorporationRating - CorporationRatingOld;
                jsonToReturn["NumberOfDialogues"] = NumberOfDialogues;

                //---working day ----
                var WorkingHours = new UserWeeklyInfo(usersInCorporation, usersInCompany)
                {
                    TotalAvg = userSessions.Sum(p => p.SessionsHours),
                    AvgPerDay = _analyticWeeklyReportUtils.AvgWorkingHoursPerDay( userSessions),
                    OfficeRating = _analyticWeeklyReportUtils.OfficeRatingWorkingHours(sessionsCompany, userId),
                    CorporationRating = _analyticWeeklyReportUtils.OfficeRatingWorkingHours(sessionsCorporation, userId)
                };
                TotalAvgOld = userSessionsOld.Sum(p => p.SessionsHours);
                OfficeRatingOld = _analyticWeeklyReportUtils.OfficeRatingWorkingHours(sessionsCompanyOld, userId);
                WorkingHours.Dynamic = WorkingHours.TotalAvg - TotalAvgOld;
                WorkingHours.OfficeRatingChanges = WorkingHours.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _analyticWeeklyReportUtils.OfficeRatingWorkingHours(sessionsCorporationOld, userId);
                WorkingHours.CorporationRatingChanges = WorkingHours.CorporationRating - CorporationRatingOld;
                jsonToReturn["WorkingHours_SessionsTotal"] = WorkingHours;

                //----time of clients work---
                var AvgDialogueTime = new UserWeeklyInfo(usersInCorporation, usersInCompany)
                {
                    TotalAvg = TimeSpan.FromHours(_analyticWeeklyReportUtils.AvgDialogueTimeTotal(userDialogues)).TotalMinutes,
                    AvgPerDay = _analyticWeeklyReportUtils.AvgDialogueTimePerDay(userDialogues),
                    OfficeRating = _analyticWeeklyReportUtils.OfficeRatingDialogueTime(dialoguesCompany, userId),
                    CorporationRating = _analyticWeeklyReportUtils.OfficeRatingDialogueTime(dialoguesCorporation, userId)
                };
                TotalAvgOld = _analyticWeeklyReportUtils.AvgDialogueTimeTotal(userDialoguesOld);
                OfficeRatingOld = _analyticWeeklyReportUtils.OfficeRatingDialogueTime(dialoguesCompanyOld, userId);
                AvgDialogueTime.Dynamic = AvgDialogueTime.TotalAvg - TimeSpan.FromHours((double)(TotalAvgOld)).TotalMinutes;
                AvgDialogueTime.OfficeRatingChanges = AvgDialogueTime.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _analyticWeeklyReportUtils.OfficeRatingDialogueTime(dialoguesCorporationOld, userId);
                AvgDialogueTime.CorporationRatingChanges = AvgDialogueTime.CorporationRating - CorporationRatingOld;
                jsonToReturn["AvgDialogueTime"] = AvgDialogueTime;
                //---load---
                var Workload = new UserWeeklyInfo(usersInCorporation, usersInCompany)
                {
                    TotalAvg = 100 * _analyticWeeklyReportUtils.WorkloadTotal(userDialogues, userSessions),
                    AvgPerDay = _analyticWeeklyReportUtils.AvgWorkloadPerDay(userDialogues, userSessions),
                    OfficeRating = _analyticWeeklyReportUtils.OfficeRatingWorkload(dialoguesCompany, sessionsCompany, userId),
                    CorporationRating = _analyticWeeklyReportUtils.OfficeRatingWorkload(dialoguesCorporation, sessionsCorporation, userId)
                };
                TotalAvgOld =  100 * _analyticWeeklyReportUtils.WorkloadTotal(userDialoguesOld, userSessionsOld);
                OfficeRatingOld = _analyticWeeklyReportUtils.OfficeRatingWorkload(dialoguesCompanyOld, sessionsCompanyOld, userId);
                Workload.Dynamic = Workload.TotalAvg - TotalAvgOld;
                Workload.OfficeRatingChanges = Workload.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _analyticWeeklyReportUtils.OfficeRatingWorkload(dialoguesCorporationOld, sessionsCorporationOld, userId);
                Workload.CorporationRatingChanges = Workload.CorporationRating - CorporationRatingOld;
                jsonToReturn["Workload"] = Workload;

                //---standarts---            
                var CrossPhrase = new UserWeeklyInfo(usersInCorporation, usersInCompany)
                {
                    TotalAvg = _analyticWeeklyReportUtils.PhraseTotalAvg(userDialogues, "CrossDialogues"),
                    AvgPerDay = _analyticWeeklyReportUtils.PhraseAvgPerDay(userDialogues, "CrossDialogues"),// userDialogues.ToDictionary(x => x.Day, i => (double)i.CrossDialogues / i.Dialogues),
                    OfficeRating = _analyticWeeklyReportUtils.OfficeRating(dialoguesCompany, userId, "CrossDialogues"),
                    CorporationRating = _analyticWeeklyReportUtils.OfficeRating(dialoguesCorporation, userId, "CrossDialogues")
                };
                TotalAvgOld = _analyticWeeklyReportUtils.PhraseTotalAvg(userDialoguesOld, "CrossDialogues");
                OfficeRatingOld = _analyticWeeklyReportUtils.OfficeRating(dialoguesCompanyOld, userId, "CrossDialogues");
                CrossPhrase.Dynamic = CrossPhrase.TotalAvg - TotalAvgOld;
                CrossPhrase.OfficeRatingChanges = CrossPhrase.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _analyticWeeklyReportUtils.OfficeRating(dialoguesCorporationOld, userId, "CrossDialogues");
                CrossPhrase.CorporationRatingChanges = CrossPhrase.CorporationRating - CorporationRatingOld;
                jsonToReturn["CrossPhrase"] = CrossPhrase;

                var AlertPhrase = new UserWeeklyInfo(usersInCorporation, usersInCompany)
                {
                    TotalAvg = _analyticWeeklyReportUtils.PhraseTotalAvg(userDialogues, "AlertDialogues"),
                    AvgPerDay =  _analyticWeeklyReportUtils.PhraseAvgPerDay(userDialogues, "AlertDialogues"),
                    OfficeRating = _analyticWeeklyReportUtils.OfficeRating(dialoguesCompany, userId, "AlertDialogues"),
                    CorporationRating = _analyticWeeklyReportUtils.OfficeRating(dialoguesCorporation, userId, "AlertDialogues")
                };
                TotalAvgOld = _analyticWeeklyReportUtils.PhraseTotalAvg(userDialoguesOld, "AlertDialogues");
                OfficeRatingOld = _analyticWeeklyReportUtils.OfficeRating(dialoguesCompanyOld, userId, "AlertDialogues");
                AlertPhrase.Dynamic = AlertPhrase.TotalAvg - TotalAvgOld;
                AlertPhrase.OfficeRatingChanges = AlertPhrase.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _analyticWeeklyReportUtils.OfficeRating(dialoguesCorporationOld, userId, "AlertDialogues");
                AlertPhrase.CorporationRatingChanges = AlertPhrase.CorporationRating - CorporationRatingOld;
                jsonToReturn["AlertPhrase"] = AlertPhrase;

                var LoyaltyPhrase = new UserWeeklyInfo(usersInCorporation, usersInCompany)
                {
                    TotalAvg =  _analyticWeeklyReportUtils.PhraseTotalAvg(userDialogues, "LoyaltyDialogues"),
                    AvgPerDay =  _analyticWeeklyReportUtils.PhraseAvgPerDay(userDialogues, "LoyaltyDialogues"),
                    OfficeRating = _analyticWeeklyReportUtils.OfficeRating(dialoguesCompany, userId, "LoyaltyDialogues"),
                    CorporationRating = _analyticWeeklyReportUtils.OfficeRating(dialoguesCorporation, userId, "LoyaltyDialogues")
                };
                TotalAvgOld = _analyticWeeklyReportUtils.PhraseTotalAvg(userDialoguesOld, "LoyaltyDialogues");
                OfficeRatingOld = _analyticWeeklyReportUtils.OfficeRating(dialoguesCompanyOld, userId, "LoyaltyDialogues");
                LoyaltyPhrase.Dynamic = LoyaltyPhrase.TotalAvg - TotalAvgOld;
                LoyaltyPhrase.OfficeRatingChanges = LoyaltyPhrase.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _analyticWeeklyReportUtils.OfficeRating(dialoguesCorporationOld, userId, "LoyaltyDialogues");
                LoyaltyPhrase.CorporationRatingChanges = LoyaltyPhrase.CorporationRating - CorporationRatingOld;
                jsonToReturn["LoyaltyPhrase"] = LoyaltyPhrase;

                var NecessaryPhrase = new UserWeeklyInfo(usersInCorporation, usersInCompany)
                {
                    TotalAvg =  _analyticWeeklyReportUtils.PhraseTotalAvg(userDialogues, "NecessaryDialogues"),
                   AvgPerDay = _analyticWeeklyReportUtils.PhraseAvgPerDay(userDialogues, "NecessaryDialogues"),
                    OfficeRating = _analyticWeeklyReportUtils.OfficeRating(dialoguesCompany, userId, "NecessaryDialogues"),
                    CorporationRating = _analyticWeeklyReportUtils.OfficeRating(dialoguesCorporation, userId, "NecessaryDialogues")
                };
                TotalAvgOld = _analyticWeeklyReportUtils.PhraseTotalAvg(userDialoguesOld, "NecessaryDialogues");
                OfficeRatingOld = _analyticWeeklyReportUtils.OfficeRating(dialoguesCompanyOld, userId, "NecessaryDialogues");
                NecessaryPhrase.Dynamic = NecessaryPhrase.TotalAvg - TotalAvgOld;
                NecessaryPhrase.OfficeRatingChanges = NecessaryPhrase.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _analyticWeeklyReportUtils.OfficeRating(dialoguesCorporationOld, userId, "NecessaryDialogues");
                NecessaryPhrase.CorporationRatingChanges = NecessaryPhrase.CorporationRating - CorporationRatingOld;
                jsonToReturn["NecessaryPhrase"] = NecessaryPhrase;

                var FillersPhrase = new UserWeeklyInfo(usersInCorporation, usersInCompany)
                {
                    TotalAvg = _analyticWeeklyReportUtils.PhraseTotalAvg(userDialoguesOld, "FillersDialogues"),
                    AvgPerDay = _analyticWeeklyReportUtils.PhraseAvgPerDay(userDialogues, "FillersDialogues"),
                    OfficeRating = _analyticWeeklyReportUtils.OfficeRating(dialoguesCompany, userId, "FillersDialogues"),
                    CorporationRating = _analyticWeeklyReportUtils.OfficeRating(dialoguesCorporation, userId, "FillersDialogues")
                };
                TotalAvgOld = _analyticWeeklyReportUtils.PhraseTotalAvg(userDialoguesOld, "FillersDialogues");
                FillersPhrase.Dynamic = FillersPhrase.TotalAvg - TotalAvgOld;

                OfficeRatingOld = _analyticWeeklyReportUtils.OfficeRating(dialoguesCompanyOld, userId, "FillersDialogues");
                FillersPhrase.OfficeRatingChanges = FillersPhrase.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _analyticWeeklyReportUtils.OfficeRating(dialoguesCorporationOld, userId, "FillersDialogues");
                FillersPhrase.CorporationRatingChanges = FillersPhrase.CorporationRating - CorporationRatingOld;
                jsonToReturn["FillersPhrase"] = FillersPhrase;

                var RiskPhrase = new UserWeeklyInfo(usersInCorporation, usersInCompany)
                {
                    TotalAvg = _analyticWeeklyReportUtils.PhraseTotalAvg(userDialoguesOld, "RiskDialogues"),
                    AvgPerDay = _analyticWeeklyReportUtils.PhraseAvgPerDay(userDialogues, "RiskDialogues"),
                    OfficeRating = _analyticWeeklyReportUtils.OfficeRating(dialoguesCompany, userId, "RiskDialogues"),
                    CorporationRating = _analyticWeeklyReportUtils.OfficeRating(dialoguesCorporation, userId, "RiskDialogues")
                };
                TotalAvgOld = _analyticWeeklyReportUtils.PhraseTotalAvg(userDialoguesOld, "RiskDialogues");
                RiskPhrase.Dynamic = RiskPhrase.TotalAvg - TotalAvgOld;

                OfficeRatingOld = _analyticWeeklyReportUtils.OfficeRating(dialoguesCompanyOld, userId, "RiskDialogues");
                RiskPhrase.OfficeRatingChanges = RiskPhrase.OfficeRating - OfficeRatingOld;

                CorporationRatingOld = _analyticWeeklyReportUtils.OfficeRating(dialoguesCorporationOld, userId, "RiskDialogues");
                RiskPhrase.CorporationRatingChanges = RiskPhrase.CorporationRating - CorporationRatingOld;
                jsonToReturn["RiskPhrase"] = RiskPhrase;

                jsonToReturn["corporationId"] = corporationId;
                return jsonToReturn;
        }

        //---PRIVATE---
        private Guid GetEmployeeRoleId()
        {
            var emplyeeRoleId = _repository.GetAsQueryable<ApplicationRole>().FirstOrDefault(x => x.Name == "Employee").Id;
            return emplyeeRoleId;
        }
        private Guid? GetCompanyId(Guid? userId)
        {
            var companyId = _repository.GetAsQueryable<ApplicationUser>().Where(p => p.Id == userId).FirstOrDefault().CompanyId;
            return companyId;
        }
        private Guid? GetCorporationId(Guid? companyId)
        {
            var corporationId = _repository.GetAsQueryable<Company>().Where(p => p.CompanyId == companyId).FirstOrDefault()?.CorporationId;
            return corporationId;
        }
        private List<Guid> GetUserIdsInCorporation(Guid? corporationId, Guid emplyeeRoleId)
        {
            var userIds = _repository.GetAsQueryable<Company>()
                .Where(p => p.CorporationId == corporationId)
                .SelectMany(p => p.ApplicationUser.Where(u => u.UserRoles.Select(r => r.RoleId)
                    .Contains(emplyeeRoleId))
                    .Select(u => u.Id))
                .ToList();
            return userIds;
        }
        private List<Guid> GetUserIdsInCompany(Guid? companyId, Guid employeeRoleId)
        {
            var userIdsInCompany = _repository.GetAsQueryable<ApplicationUser>()
                .Where(p => p.CompanyId == companyId
                    && p.UserRoles.Select(r => r.RoleId).Contains(employeeRoleId))
                .Select(u => u.Id)
                .ToList();
            return userIdsInCompany;
        }
        private List<VSessionUserWeeklyReport> GetSessionMoreThanBegTime(List<Guid> userIds, DateTime begTime)
        {
            var sessionCorporation = _repository.GetAsQueryable<VSessionUserWeeklyReport>()
                .Where(p => userIds.Contains(p.AspNetUserId)
                    && p.Day > begTime)
                .ToList();
            return sessionCorporation;
        }

        private List<VSessionUserWeeklyReport> GetSessionLessThanBegTime(List<Guid> userIds, DateTime begTime)
        {
            var sessionCorporation = _repository.GetAsQueryable<VSessionUserWeeklyReport>()
                .Where(p => userIds.Contains(p.AspNetUserId)
                    && p.Day <= begTime)
                .ToList();
            return sessionCorporation;
        }
        private List<VWeeklyUserReport> GetDialoguesMoreThanBegTime(List<Guid> userIds, DateTime begTime)
        {
            var dialoguesCorporation = _repository.GetAsQueryable<VWeeklyUserReport>()
                .Where(p => userIds.Contains(p.AspNetUserId)
                    && p.Day > begTime)
                .ToList();
            return dialoguesCorporation;
        }
        private List<VWeeklyUserReport> GetDialoguesLessThanBegTime(List<Guid> userIds, DateTime begTime)
        {
            var dialogueCorporation = _repository.GetAsQueryable<VWeeklyUserReport>()
                .Where(p => userIds.Contains(p.AspNetUserId)
                    && p.Day <= begTime)
                .ToList();
            return dialogueCorporation;
        }

    }
}