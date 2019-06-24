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

        public void CheckRoles(ref List<Guid> companyIds, List<Guid> corporationIds, string role, Guid companyId)
        {
            List<Guid> compIds = companyIds;
            //--- admin can view any companies in any corporation
                   if ( role == "Admin" )
                    {             
                        //---take all companyIds in filter or all company ids in corporations
                        if (!compIds.Any() && !corporationIds.Any()) 
                        {
                        compIds =  _context.Companys
                            //.Where(x => x.StatusId == 3)
                            .Select(x => x.CompanyId).ToList();
                        }
                        else if (!compIds.Any())
                        {
                        compIds = _context.Companys
                            .Where(x => corporationIds.Contains( (Guid)x.CorporationId ))
                            .Select(x => x.CompanyId).ToList();
                        }
                    }
                    //--- supervisor can view companies from filter or companies from own corporation -------
                   else if ( role == "Supervisor" )
                   {
                        if (!compIds.Any())//--- if filter by companies not set ---
                        {//--- select own corporation
                            var corporation = _context.Companys.Include(x => x.Corporation).Where(x => x.CompanyId == companyId).FirstOrDefault().Corporation;
                        //--- return all companies from own corporation
                            compIds = _context.Companys.Where(x => x.CorporationId == corporation.Id ).Select(x => x.CompanyId).ToList();
                        }
                   }                 
                    //--- for simple user return only for own company
                   else
                    {
                        compIds = new List<Guid> { companyId };
                    }
                    companyIds = compIds;
        }

        public List<Guid> IndustryIdsForCompany(List<Guid> companyIds)
        {
            return _context.Companys
                    .Include(x => x.CompanyIndustry)
                    .Where(x => companyIds.Contains( x.CompanyId ) 
                    && x.CompanyIndustryId != null)
                    .Select(x => (Guid)x.CompanyIndustryId)
                    .Distinct()
                    .ToList();            
        }

        public List<Guid> CompanyIdsInIndustryExceptSelected(List<Guid> companyIds)
        {
            List<Guid> companyIndustryIds = IndustryIdsForCompany(companyIds);
            return _context.Companys                    
                    .Where(x => !companyIds.Contains( x.CompanyId ) 
                     && x.CompanyIndustryId != null
                    && companyIndustryIds.Contains((Guid)x.CompanyIndustryId))
                    .Select(x => x.CompanyId)
                    .ToList();            
        }

        public List<Guid> CompanyIdsInIndustry(List<Guid> companyIds)
        {
            List<Guid> companyIndustryIds = IndustryIdsForCompany(companyIds);
            return _context.Companys                    
                    .Where(x => 
                    x.CompanyIndustryId != null
                    && companyIndustryIds.Contains((Guid)x.CompanyIndustryId))
                    .Select(x => x.CompanyId)
                    .ToList();            
        }

        public List<Guid> CompanyIdsInHeedbookExceptSelected(List<Guid> companyIds)
        {
            List<Guid> companyIndustryIds = IndustryIdsForCompany(companyIds);
            return _context.Companys                    
                    .Where(x => !companyIds.Contains( x.CompanyId ))
                    .Select(x => x.CompanyId)
                    .ToList();            
        }
        
    }
}