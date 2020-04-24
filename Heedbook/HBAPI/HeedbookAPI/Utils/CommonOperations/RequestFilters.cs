using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using HBData;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using UserOperations.Controllers;
using UserOperations.Services;
using HBData.Repository;
using HBData.Models;

namespace UserOperations.Utils
{
    public class RequestFilters : IRequestFilters
    {
        private readonly IGenericRepository _repository;
        private readonly ILoginService _loginService;

        public RequestFilters(IGenericRepository repository, IConfiguration config, ILoginService loginService)
        {
            _repository = repository;
            _loginService = loginService;
        }

        public DateTime GetBegDate(string beg)
        {
            try
            {
                var stringFormat = "yyyyMMdd";
                var begTime = !String.IsNullOrEmpty(beg) ? DateTime.ParseExact(beg, stringFormat, CultureInfo.InvariantCulture) : DateTime.Now.AddDays(-6);
                return begTime.Date;
            }
            catch
            {
                throw new FormatException("wrong date format");
            }
        }
        public DateTime GetEndDate(string end)
        {
            try
            {
                var stringFormat = "yyyyMMdd";
                var endTime = !String.IsNullOrEmpty(end) ? DateTime.ParseExact(end, stringFormat, CultureInfo.InvariantCulture) : DateTime.Now;
                return endTime.Date.AddDays(1);
            }
            catch
            {
                throw new FormatException("wrong date format");
            }
        }
        
        public bool IsCompanyBelongToUser(Guid? corporationIdInToken, Guid? companyIdInToken, Guid? companyIdInParams, string roleInToken)
        {
            var isAdmin = roleInToken == "Admin";
            var isSupervisor = roleInToken == "Supervisor";
            if (isAdmin) return true;

            if (isSupervisor && IsCompanyBelongToCorporation(corporationIdInToken, companyIdInParams) == false)
                throw new AccessException("No access");
            if (!isSupervisor && (companyIdInParams == null || companyIdInParams == Guid.Empty || companyIdInToken != companyIdInParams))
                throw new AccessException("No access");
            return true;
        }

        public bool IsCompanyBelongToUser(Guid companyIdInParams)
        {
            var roleInToken = _loginService.GetCurrentRoleName();
            var companyIdInToken = _loginService.GetCurrentCompanyId();
            var corporationIdInToken = _loginService.GetCurrentCorporationId();

            var isAdmin = roleInToken == "Admin";
            var isSupervisor = roleInToken == "Supervisor";
            if (isAdmin) return true;

            if (isSupervisor && IsCompanyBelongToCorporation(corporationIdInToken, companyIdInParams) == false)
                throw new AccessException("No access");
            if (!isSupervisor && (companyIdInParams == null || companyIdInParams == Guid.Empty || companyIdInToken != companyIdInParams))
                throw new AccessException("No access");
            return true;
        }
        public void CheckRolesAndChangeCompaniesInFilter(ref List<Guid> companyIdsInFilter, List<Guid> corporationIdsInFilter)
        {
            var roleInToken = _loginService.GetCurrentRoleName();
            var companyIdInToken = _loginService.GetCurrentCompanyId();
            CheckRolesAndChangeCompaniesInFilter(ref companyIdsInFilter, corporationIdsInFilter, roleInToken, companyIdInToken);
        }

        public void CheckRolesAndChangeCompaniesInFilter(ref List<Guid> companyIdsInFilter, List<Guid> corporationIdsInFilter, string role, Guid companyIdInToken)
        {
            //--- admin can view any companies in any corporation
            if (role == "Admin")
            {
                //---take all companyIds in filter or all company ids in corporations
                if (!companyIdsInFilter.Any() && (corporationIdsInFilter == null || !corporationIdsInFilter.Any()))
                {
                    companyIdsInFilter = _repository.GetAsQueryable<Company>()
                        //.Where(x => x.StatusId == 3)
                        .Select(x => x.CompanyId).ToList();
                }
                else if (!companyIdsInFilter.Any())// means corporationIdsInFilter not null
                {
                    companyIdsInFilter = _repository.GetAsQueryable<Company>()
                        .Where(x => corporationIdsInFilter.Contains((Guid)x.CorporationId))
                        .Select(x => x.CompanyId).ToList();
                }
            }
            //--- supervisor can view companies from filter or companies from own corporation -------
            else if (role == "Supervisor")
            {
                if (!companyIdsInFilter.Any())//--- if filter by companies not set ---
                {//--- select own corporation
                    companyIdsInFilter = _repository.GetAsQueryable<Company>()
                        .Include(p => p.Corporation)
                        .Where(p => p.CompanyId == companyIdInToken)
                        .SelectMany(p => _repository.GetAsQueryable<Company>().Where(x => p.Corporation != null
                            && x.CorporationId == p.Corporation.Id)
                        .Select(x => x.CompanyId))
                        .ToList();

                    if (companyIdsInFilter.Count == 0)
                        companyIdsInFilter = new List<Guid> { companyIdInToken };
                }
            }
            //--- for simple user return only for own company
            else
            {
                companyIdsInFilter = new List<Guid> { companyIdInToken };
            }
        }

        //---PRIVATE---
        private bool IsCompanyBelongToCorporation(Guid? corporationIdInToken, Guid? companyId)
        {
            if (corporationIdInToken == null || corporationIdInToken == Guid.Empty) return true;
            if (companyId == null || companyId == Guid.Empty) return false;
            var companiesInCorporation = _repository.GetAsQueryable<Company>().Where(p => p.CorporationId == corporationIdInToken)
                         .Select(p => p.CompanyId)
                         .ToList();
            return companiesInCorporation.Contains((Guid)companyId);
        }



        //public List<Guid> IndustryIdsForCompany(List<Guid> companyIds)
        //{
        //    return _context.Companys
        //            .Include(x => x.CompanyIndustry)
        //            .Where(x => companyIds.Contains(x.CompanyId)
        //            && x.CompanyIndustryId != null)
        //            .Select(x => (Guid)x.CompanyIndustryId)
        //            .Distinct()
        //            .ToList();
        //}
    }
}