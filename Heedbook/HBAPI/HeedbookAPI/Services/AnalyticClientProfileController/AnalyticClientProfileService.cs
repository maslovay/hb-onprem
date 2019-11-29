using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using HBData;
using UserOperations.Services;
using Newtonsoft.Json;
using UserOperations.Utils;
using Swashbuckle.AspNetCore.Annotations;
using UserOperations.Providers;
using System.Threading.Tasks;
using static UserOperations.Models.AnalyticModels.ClientProfileModels;

namespace UserOperations.Services
{
    public class AnalyticClientProfileService : Controller
    {
        private readonly IAnalyticCommonProvider _analyticProvider;
        private readonly ILoginService _loginService;
        private readonly IDBOperations _dbOperation;
        private readonly IRequestFilters _requestFilters;
        private readonly List<AgeBoarder> _ageBoarders;

        public AnalyticClientProfileService(
            IAnalyticCommonProvider analyticProvider,
            ILoginService loginService,
            IDBOperations dbOperation,
            IRequestFilters requestFilters
            )
        {
            _analyticProvider = analyticProvider;
            _loginService = loginService;
            _dbOperation = dbOperation;
            _requestFilters = requestFilters;
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
        public async Task<IActionResult> EfficiencyDashboard([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end,
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                var begYearTime = endTime.AddYears(-1);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);

                var persondIdsPerYear = await _analyticProvider.GetPersondIdsAsync(begYearTime, begTime, companyIds);
                var data = _analyticProvider.GetDialoguesIncludedClientProfile(begTime, endTime, companyIds, applicationUserIds, workerTypeIds)
                    .Select(p => new
                    {
                        p.DialogueClientProfile.FirstOrDefault().Age,
                        p.DialogueClientProfile.FirstOrDefault().Gender,
                        p.PersonId,
                        p.DialogueId
                    }).ToList();

                var result = new List<GenderAgeStructureResult>();                
                result = _ageBoarders.Select(p => 
                        {
                            var dataBoarders = data.Where(b => b.Age > p.BegAge && b.Age <= p.EndAge);
                            return new GenderAgeStructureResult
                            {                            
                                Age = $"{p.BegAge}-{p.EndAge}",
                                MaleCount = dataBoarders
                                    .Where(d => d.Gender == "male")
                                    .Count(),
                                FemaleCount = dataBoarders
                                    .Where(d => d.Gender == "female")
                                    .Count(),
                                MaleAverageAge = dataBoarders
                                    .Where(d => d.Gender == "male")
                                    .Select(d => d.Age)
                                    .Average(),
                                FemaleAverageAge = dataBoarders
                                    .Where(d => d.Gender == "female")
                                    .Select(d => d.Age)
                                    .Average()
                            };
                        })
                    .ToList();
                var jsonToReturn = new Dictionary<string, object>();
                jsonToReturn["allClients"] = data.Select(p => p.DialogueId).Distinct().Count();
                jsonToReturn["uniquePerYearClients"] = data
                    .Where(p => p.PersonId != null && !persondIdsPerYear.Contains(p.PersonId))
                    .Select(p => p.PersonId).Distinct().Count() + data.Where(p => p.PersonId == null).Select(p => p.DialogueId).Distinct().Count();
                jsonToReturn["genderAge"] = result;
                return Ok(JsonConvert.SerializeObject(jsonToReturn));
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }
    }
}