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
using HBLib.Utils;

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
        private readonly RequestFilters _requestFilters;
        private readonly List<AgeBoarder> _ageBoarders;
        // private readonly ElasticClient _log;

        public AnalyticClientProfileController(
            IConfiguration config,
            ILoginService loginService,
            RecordsContext context,
            DBOperations dbOperation,
            RequestFilters requestFilters
            // ElasticClient log
            )
        {
            _config = config;
            _loginService = loginService;
            _context = context;
            _dbOperation = dbOperation;
            _requestFilters = requestFilters;
            // _log = log;
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
        [SwaggerOperation(Summary = "Return data on dashboard", Description = "For admins ignore companyId filter")]
        [SwaggerResponse(200, "GenderAgeStructureResult", typeof(List<GenderAgeStructureResult>))]
        public IActionResult EfficiencyDashboard([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end,
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                // _log.Info("AnalyticClientProfile/GenderAgeStructure started");
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);     
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                var begYearTime = endTime.AddYears(-1);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);   

                var persondIdsPerYear = _context.Dialogues
                    .Where(p => p.BegTime >= begYearTime &&
                        p.EndTime < begTime &&
                        p.StatusId == 3 &&
                        p.InStatistic == true &&
                        p.PersonId != null &&
                        (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId)))
                    .Select(p => p.PersonId).Distinct()
                    .ToList();

                var data = _context.Dialogues
                    .Include(p => p.DialogueClientProfile)
                    .Include(p => p.ApplicationUser)
                    .Where(p => p.BegTime >= begTime &&
                        p.EndTime <= endTime &&
                        p.StatusId == 3 &&
                        p.InStatistic == true &&
                        (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId)) &&
                        (!applicationUserIds.Any() || applicationUserIds.Contains((Guid)p.ApplicationUserId)) &&
                        (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)))
                    .Select(p => new
                    {
                        Age = p.DialogueClientProfile.FirstOrDefault().Age,
                        Gender = p.DialogueClientProfile.FirstOrDefault().Gender,
                        PersonId = p.PersonId,
                        p.DialogueId
                    }).ToList();

                var result = new List<GenderAgeStructureResult>();
                foreach (var ageBoarder in _ageBoarders)
                {
                    var dataBoarders = data
                        .Where(p => p.Age > ageBoarder.BegAge && p.Age <= ageBoarder.EndAge)
                        .ToList();
                    result.Add(new GenderAgeStructureResult
                    {
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
                var jsonToReturn = new Dictionary<string, object>();
               // jsonToReturn["allClients"] = data.Where(p => p.PersonId != null).Select(p => p.PersonId).Distinct().Count();
                jsonToReturn["allClients"] = data.Select(p => p.DialogueId).Distinct().Count();
                jsonToReturn["uniquePerYearClients"] = data
                    .Where(p => p.PersonId != null && !persondIdsPerYear.Contains(p.PersonId))
                    .Select(p => p.PersonId).Distinct().Count() + data.Where(p => p.PersonId == null).Select(p => p.DialogueId).Distinct().Count();
                jsonToReturn["genderAge"] = result;

                // _log.Info("AnalyticClientProfile/GenderAgeStructure finished");
                return Ok(JsonConvert.SerializeObject(jsonToReturn));
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred {e}");
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
