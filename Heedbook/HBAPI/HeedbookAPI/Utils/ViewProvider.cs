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
// using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Data.Common;
using System.Data;

namespace UserOperations.Utils
{
    public class ViewProvider
    {
        private readonly RecordsContext _context;
        //    private readonly IConfiguration _config;
        // private readonly IDistributedCache _distributedCache;
        public ViewProvider(RecordsContext context)
        {
            _context = context;
            //     _config = config;
            // _distributedCache = distributedCache;
        }


        public IEnumerable<VIndexByCompanyDay> VIndexesByCompanysDays
        {
            get
            {
                var query = "select * from public.\"VIndexesByCompanysDays\"";
                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = query;
                    command.CommandType = CommandType.Text;
                    _context.Database.OpenConnection();
                    using (var result = command.ExecuteReader())
                    {
                        var entities = new List<VIndexByCompanyDay>();
                        while (result.Read())
                        {
                            Console.WriteLine(result[0]);
                            if (result != null)
                            {
                                var ind = new VIndexByCompanyDay
                                {
                                    CompanyId = result[1] != null ? (Guid)result[1] : default(Guid),
                                    CompanyIndustryId = result[2] != null ? (Guid)result[2] : default(Guid),
                                    Day = result[3]!= null?  (DateTime)result[3] : default(DateTime),
                                    SatisfactionIndex = result[4]!= null? Convert.ToDouble(result[4]): 0,
                                    DialoguesHours = result[5]!= null? Convert.ToDouble(result[5]): 0,
                                   // SessionHours = result[6]!= null? Convert.ToDouble(result[6]): 0
                                };
                                entities.Add(ind);
                            }
                        }
                        return entities;
                    }
                }
            }
        }
    }
}