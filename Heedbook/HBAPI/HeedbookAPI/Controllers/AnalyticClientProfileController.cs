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

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticClientProfileController : Controller
    {
        private readonly AnalyticClientProfileService _analyticClientProfileService;
        public AnalyticClientProfileController(
            ILoginService loginService,
            AnalyticClientProfileService analyticClientProfileService)
        {
            _analyticClientProfileService = analyticClientProfileService;
        }


        [HttpGet("GenderAgeStructure")]
        [SwaggerOperation(Summary = "Return data on dashboard", Description = "For admins ignore companyId filter")]
        [SwaggerResponse(200, "GenderAgeStructureResult", typeof(List<GenderAgeStructureResult>))]
        public async Task<IActionResult> EfficiencyDashboard([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end,
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization) 
            => await _analyticClientProfileService.EfficiencyDashboard(
                beg,
                end,
                applicationUserIds,
                companyIds,
                corporationIds,
                workerTypeIds,
                Authorization);        
    }
}
