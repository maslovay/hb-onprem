using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Services;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;
using UserOperations.Models.Get.AnalyticClientProfileController;
using Microsoft.AspNetCore.Authorization;
using UserOperations.Utils;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ControllerExceptionFilter]
    public class AnalyticClientProfileController : Controller
    {
        private readonly AnalyticClientProfileService _analyticClientProfileService;
        public AnalyticClientProfileController(
            AnalyticClientProfileService analyticClientProfileService)
        {
            _analyticClientProfileService = analyticClientProfileService;
        }


        [HttpGet("GenderAgeStructure")]
        [SwaggerOperation(Summary = "Return data on dashboard", Description = "For admins ignore companyId filter")]
        [SwaggerResponse(200, "GenderAgeStructureResult", typeof(List<GenderAgeStructureResult>))]
        public async Task<string> EfficiencyDashboard([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end,
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid?> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "deviceId[]")] List<Guid> deviceIds)
            => await _analyticClientProfileService.EfficiencyDashboard(
                beg,
                end,
                applicationUserIds,
                companyIds,
                corporationIds,
                deviceIds);
    }
}
