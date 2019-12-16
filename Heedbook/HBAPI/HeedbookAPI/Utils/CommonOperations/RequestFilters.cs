using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;
using HBData.Models;
using HBData.Models.AccountViewModels;
using HBData;
using UserOperations.Services;
using UserOperations.Models.AnalyticModels;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using UserOperations.Utils;
using Swashbuckle.AspNetCore.Annotations;
using UserOperations.Controllers;

namespace UserOperations.Utils
{
    public class RequestFilters
    {
        private readonly RecordsContext _context;
        private readonly IConfiguration _config;

        public RequestFilters(RecordsContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
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
            if (isSupervisor && IsCompanyBelongToCorporation(corporationIdInToken, companyIdInParams) == false)
                throw new AccessException("No access");
            if (isAdmin) return true;
            if (!isSupervisor &&  (companyIdInParams == null || companyIdInToken != companyIdInParams))
                throw new AccessException("No access");
            return true;
        }

        public void CheckRolesAndChangeCompaniesInFilter(ref List<Guid> companyIdsInFilter, List<Guid> corporationIdsInFilter, string role, Guid companyIdInToken)
        {
            //--- admin can view any companies in any corporation
            if (role == "Admin")
            {
                //---take all companyIds in filter or all company ids in corporations
                if (!companyIdsInFilter.Any() && (corporationIdsInFilter ==null || !corporationIdsInFilter.Any()))
                {
                    companyIdsInFilter = _context.Companys
                        //.Where(x => x.StatusId == 3)
                        .Select(x => x.CompanyId).ToList();
                }
                else if (!companyIdsInFilter.Any())// means corporationIdsInFilter not null
                {
                    companyIdsInFilter = _context.Companys
                        .Where(x => corporationIdsInFilter.Contains((Guid)x.CorporationId))
                        .Select(x => x.CompanyId).ToList();
                }
            }
            //--- supervisor can view companies from filter or companies from own corporation -------
            else if (role == "Supervisor")
            {
                if (!companyIdsInFilter.Any())//--- if filter by companies not set ---
                {//--- select own corporation
                    companyIdsInFilter = _context.Companys
                        .Include(p => p.Corporation)
                        .Where(p => p.CompanyId == companyIdInToken)
                        .SelectMany(p => _context.Companys.Where(x => p.Corporation != null
                            && x.CorporationId == p.Corporation.Id)
                        .Select(x => x.CompanyId))
                        .ToList();
                    
                    if(companyIdsInFilter.Count == 0)
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
           var companiesInCorporation =  _context.Companys.Where(p => p.CorporationId == corporationIdInToken)
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