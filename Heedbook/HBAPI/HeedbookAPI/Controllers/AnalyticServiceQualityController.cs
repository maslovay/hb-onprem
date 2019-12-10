using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Services;
using System.Threading.Tasks;
//---OLD---
namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
        public async Task<IActionResult> ServiceQualityComponents([FromQuery(Name = "begTime")] string beg,
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
                workerTypeIds,
                Authorization);
        

        [HttpGet("Dashboard")]
        public IActionResult ServiceQualityDashboard([FromQuery(Name = "begTime")] string beg,
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
                workerTypeIds,
                Authorization);
        

        [HttpGet("Rating")]
        public async Task<IActionResult> ServiceQualityRating([FromQuery(Name = "begTime")] string beg,
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
                workerTypeIds,
                Authorization);
        

        [HttpGet("SatisfactionStats")]
        public async Task<IActionResult> ServiceQualitySatisfactionStats([FromQuery(Name = "begTime")] string beg,
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
                workerTypeIds,
                Authorization);   
    }
}