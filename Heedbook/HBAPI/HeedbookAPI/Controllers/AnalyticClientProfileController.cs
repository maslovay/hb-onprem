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

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticClientProfileController : Controller
    {
        private readonly IConfiguration _config;             
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
        private readonly DBOperations _dbOperation;
        private readonly List<AgeBoarder> _ageBoarders;

        public AnalyticClientProfileController(
            IConfiguration config,
            ILoginService loginService,
            RecordsContext context,
            DBOperations dbOperation
            )
        {
            _config = config;
            _loginService = loginService;
            _context = context;
            _dbOperation = dbOperation;
            _ageBoarders = new List<AgeBoarder>{ 
                new AgeBoarder{
                    BegAge = 0,
                    EndAge = 21
                },
                new AgeBoarder {
                    BegAge = 21,
                    EndAge = 35
                },
                new AgeBoarder {
                    BegAge = 35,
                    EndAge = 55
                },
                new AgeBoarder {
                    BegAge = 55,
                    EndAge = 100
                }};
        }
        

        [HttpGet("GenderAgeStructure")]
        public IActionResult EfficiencyDashboard([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                companyIds = !companyIds.Any()? new List<Guid> { Guid.Parse(userClaims["companyId"])} : companyIds;

                var stringFormat = "yyyyMMdd";
                var begTime = !String.IsNullOrEmpty(beg) ? DateTime.ParseExact(beg, stringFormat, CultureInfo.InvariantCulture) : DateTime.Now.AddDays(-6);
                var endTime = !String.IsNullOrEmpty(end) ? DateTime.ParseExact(end, stringFormat, CultureInfo.InvariantCulture) : DateTime.Now;
                begTime = begTime.Date;
                endTime = endTime.Date.AddDays(1);

                
                var data = _context.Dialogues
                    .Include(p => p.DialogueClientProfile)
                    .Include(p => p.ApplicationUser)
                    .Where(p => p.BegTime >= begTime &&
                        p.EndTime <= endTime &&
                        p.StatusId == 3 &&
                        p.InStatistic == true &&
                        (!companyIds.Any() || companyIds.Contains((Guid) p.ApplicationUser.CompanyId)) &&
                        (!applicationUserIds.Any() || applicationUserIds.Contains((Guid) p.ApplicationUserId)) &&
                        (!workerTypeIds.Any() || workerTypeIds.Contains((Guid) p.ApplicationUser.WorkerTypeId)) )
                    .Select(p => new {
                        Age = p.DialogueClientProfile.FirstOrDefault().Age,
                        Gender = p.DialogueClientProfile.FirstOrDefault().Gender
                    })
                    .ToList();

                var result = new List<GenderAgeStructureResult>();
                foreach (var ageBoarder in _ageBoarders)
                {
                    var dataBoarders = data
                        .Where(p => p.Age > ageBoarder.BegAge && p.Age <= ageBoarder.EndAge)
                        .ToList();
                    result.Add(new GenderAgeStructureResult{
                        Age = $"{ageBoarder.BegAge}-{ageBoarder.EndAge}",
                        MaleCount = dataBoarders
                            .Where(p => p.Gender == "male")
                            .Count(),
                        FemaleCount = dataBoarders
                            .Where(p => p.Gender == "female")
                            .Count(),
                        MaleAverageAge = dataBoarders
                            .Where(p => p.Gender == "male")
                            .Select(p => p.Age)
                            .Average(),
                        FemaleAverageAge = dataBoarders
                            .Where(p => p.Gender == "female")
                            .Select(p => p.Age)
                            .Average()
                    });
                }
                return Ok(JsonConvert.SerializeObject(result));
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }

        }
    }

    public class AgeBoarder
    {
        public int BegAge;
        public int EndAge;
    }

    public class GenderAgeStructureResult
    {
        public string Age { get; set; }

        public int MaleCount { get; set; }

        public int FemaleCount { get; set; }

        public double? MaleAverageAge { get; set; }

        public double? FemaleAverageAge { get; set; }
    }
}
