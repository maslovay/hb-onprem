using System;
using System.Collections.Generic;
using System.Linq;
using HBData.Models;
using HBData.Repository;

namespace UserOperations.Providers
{
    public class AnalyticWeeklyReportProvider : IAnalyticWeeklyReportProvider
    {
        private readonly IGenericRepository _repository;
        public AnalyticWeeklyReportProvider(IGenericRepository repository)
        {
            _repository = repository;
        }
        public Guid GetEmployeeRoleId()
        {
            var emplyeeRoleId = _repository.GetAsQueryable<ApplicationRole>().FirstOrDefault(x => x.Name == "Employee").Id;
            return emplyeeRoleId;
        }
        public Guid? GetCompanyId(Guid? userId)
        {
            var companyId = _repository.GetAsQueryable<ApplicationUser>().Where(p => p.Id == userId).FirstOrDefault().CompanyId;
            return companyId;
        }
        public Guid? GetCorporationId(Guid? companyId)
        {
            var corporationId = _repository.GetAsQueryable<Company>().Where(p => p.CompanyId == companyId).FirstOrDefault()?.CorporationId;
            return corporationId;
        }
        public List<Guid> GetUserIdsInCorporation(Guid? corporationId, Guid emplyeeRoleId)
        {
            var userIds = _repository.GetAsQueryable<Company>()
                .Where(p => p.CorporationId == corporationId)
                .SelectMany(p => p.ApplicationUser.Where(u => u.UserRoles.Select(r => r.RoleId)
                    .Contains(emplyeeRoleId))
                    .Select(u => u.Id))
                .ToList();
            return userIds;
        }
        public List<Guid> GetUserIdsInCompany(Guid? companyId, Guid employeeRoleId)
        {
            var userIdsInCompany = _repository.GetAsQueryable<ApplicationUser>()                
                .Where(p => p.CompanyId == companyId 
                    && p.UserRoles.Select(r => r.RoleId).Contains(employeeRoleId))
                .Select(u => u.Id)
                .ToList();
            return userIdsInCompany;
        }
        public List<VSessionUserWeeklyReport> GetSessionMoreThanBegTime(List<Guid> userIds, DateTime begTime)
        {
            var sessionCorporation = _repository.GetAsQueryable<VSessionUserWeeklyReport>()
                .Where(p => userIds.Contains(p.AspNetUserId) 
                    && p.Day > begTime)
                .ToList();
            return sessionCorporation;
        }
        
        public List<VSessionUserWeeklyReport> GetSessionLessThanBegTime(List<Guid> userIds, DateTime begTime)
        {
            var sessionCorporation = _repository.GetAsQueryable<VSessionUserWeeklyReport>()
                .Where(p => userIds.Contains(p.AspNetUserId) 
                    && p.Day <= begTime)
                .ToList();
            return sessionCorporation;
        }
        public List<VWeeklyUserReport> GetDialoguesMoreThanBegTime(List<Guid> userIds, DateTime begTime)
        {
            var dialoguesCorporation = _repository.GetAsQueryable<VWeeklyUserReport>()
                .Where(p => userIds.Contains(p.AspNetUserId) 
                    && p.Day > begTime)
                .ToList();
            return dialoguesCorporation;
        }
        public List<VWeeklyUserReport> GetDialoguesLessThanBegTime(List<Guid> userIds, DateTime begTime)
        {
            var dialogueCorporation = _repository.GetAsQueryable<VWeeklyUserReport>()
                .Where(p => userIds.Contains(p.AspNetUserId) 
                    && p.Day <= begTime)
                .ToList();
            return dialogueCorporation;
        }
    }
}