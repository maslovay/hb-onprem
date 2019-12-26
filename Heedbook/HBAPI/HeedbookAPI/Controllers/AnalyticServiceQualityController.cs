using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ControllerExceptionFilter]
    public class AnalyticServiceQualityController : Controller
    {
        private readonly AnalyticServiceQualityService _analyticServiceQualityService;

        public AnalyticServiceQualityController(
            AnalyticServiceQualityService analyticServiceQualityService
            )
        {
            _analyticServiceQualityService = analyticServiceQualityService;
        }

        [HttpGet("Components")]
        public async Task<string> ServiceQualityComponents([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization) => 
            await _analyticServiceQualityService.ServiceQualityComponents(
                beg,
                end,
                applicationUserIds,
                companyIds,
                corporationIds,
                workerTypeIds);
        

        [HttpGet("Dashboard")]
        public string ServiceQualityDashboard([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization) => 
            _analyticServiceQualityService.ServiceQualityDashboard(
                beg,
                end,
                applicationUserIds,
                companyIds,
                corporationIds,
                workerTypeIds);
        

        [HttpGet("Rating")]
        public async Task<string> ServiceQualityRating([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization) =>
            await _analyticServiceQualityService.ServiceQualityRating(
                beg,
                end,
                applicationUserIds,
                companyIds,
                corporationIds,
                workerTypeIds);
        

        [HttpGet("SatisfactionStats")]
        public async Task<string> ServiceQualitySatisfactionStats([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization) =>
            await _analyticServiceQualityService.ServiceQualitySatisfactionStats(
                beg,
                end,
                applicationUserIds,
                companyIds,
                corporationIds,
                workerTypeIds);
    }
}