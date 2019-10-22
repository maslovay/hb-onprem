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
            var stringFormat = "yyyyMMdd";
            var begTime = !String.IsNullOrEmpty(beg) ? DateTime.ParseExact(beg, stringFormat, CultureInfo.InvariantCulture) : DateTime.Now.AddDays(-6);
            return begTime.Date;
        }
        public DateTime GetEndDate(string end)
        {
            var stringFormat = "yyyyMMdd";
            var endTime = !String.IsNullOrEmpty(end) ? DateTime.ParseExact(end, stringFormat, CultureInfo.InvariantCulture) : DateTime.Now;
            return endTime.Date.AddDays(1);
        }

        public List<Guid> GetAllowedRoles(string roleInToken)
        {
            var allRoles = _context.Roles.ToList();
            var isAdmin = roleInToken == "Admin";
            var isManager = roleInToken == "Manager";
            var isSupervisor = roleInToken == "Supervisor";

            if (isAdmin)
            {
                return allRoles.Where(p => p.Name != "Admin").Select(x => x.Id).ToList();
            }
            if (isSupervisor)
            {
                return allRoles.Where(p => p.Name != "Admin" && p.Name != "Teacher").Select(x => x.Id).ToList();
            }
            if (isManager)
            {
                return allRoles.Where(p => p.Name != "Admin" && p.Name != "Teacher" && p.Name != "Supervisor").Select(x => x.Id).ToList();
            }
            return null;
        }

        public string CheckAndGetAllowedRole(string roleForUserInParams, string roleInToken)
        {
            var isAdmin = roleInToken == "Admin";
            var isManager = roleInToken == "Manager";
            var isSupervisor = roleInToken == "Supervisor";
            return roleForUserInParams != null && (isAdmin || isSupervisor) ? roleForUserInParams : _context.Roles.FirstOrDefault(x => x.Name == "Employee").Id.ToString();
        }

        private bool IsCompanyBelongToCorporation(string corporationIdInToken, string companyId)
        {
            if (corporationIdInToken == null) return true;
            if (companyId == null) return false;
           var companiesInCorporation =  _context.Companys.Where(p => p.CorporationId.ToString() == corporationIdInToken)
                        .Select(p => p.CompanyId)
                        .ToList();
            return !companiesInCorporation.Contains(Guid.Parse(companyId));
        }


        public bool IsCompanyBelongToUser(string corporationIdInToken, string companyIdInToken, string companyIdInParams, string roleInToken)
        {
            var isAdmin = roleInToken == "Admin";
            var isManager = roleInToken == "Manager";
            var isSupervisor = roleInToken == "Supervisor";
            if (isSupervisor && IsCompanyBelongToCorporation(corporationIdInToken, companyIdInParams) == false)
                    return false;
            if (isManager && (companyIdInParams == null || companyIdInToken != companyIdInParams))
                return false;
            if (isAdmin) return true;
            return true;
        }   

        public void CheckRolesAndChangeCompaniesInFilter(ref List<Guid> companyIdsInFilter, List<Guid> corporationIdsInFilter, string role, Guid companyIdInToken)
        {
            List<Guid> companyIdsForResult = companyIdsInFilter;
            //--- admin can view any companies in any corporation
            if (role == "Admin")
            {
                //---take all companyIds in filter or all company ids in corporations
                if (!companyIdsForResult.Any() && !corporationIdsInFilter.Any())
                {
                    companyIdsForResult = _context.Companys
                        //.Where(x => x.StatusId == 3)
                        .Select(x => x.CompanyId).ToList();
                }
                else if (!companyIdsForResult.Any())
                {
                    companyIdsForResult = _context.Companys
                        .Where(x => corporationIdsInFilter.Contains((Guid)x.CorporationId))
                        .Select(x => x.CompanyId).ToList();
                }
            }
            //--- supervisor can view companies from filter or companies from own corporation -------
            else if (role == "Supervisor")
            {
                if (!companyIdsForResult.Any())//--- if filter by companies not set ---
                {//--- select own corporation
                    var corporation = _context.Companys.Include(x => x.Corporation).Where(x => x.CompanyId == companyIdInToken).FirstOrDefault()?.Corporation;
                    //--- return all companies from own corporation
                    if (corporation != null)
                        companyIdsForResult = _context.Companys.Where(x => x.CorporationId == corporation.Id).Select(x => x.CompanyId).ToList();
                    else
                        companyIdsForResult = new List<Guid> { companyIdInToken };
                }
            }
            //--- for simple user return only for own company
            else
            {
                companyIdsForResult = new List<Guid> { companyIdInToken };
            }
            companyIdsInFilter = companyIdsForResult;
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