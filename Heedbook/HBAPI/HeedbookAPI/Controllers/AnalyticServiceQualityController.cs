using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using UserOperations.Utils;
using HBLib.Utils;

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
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid?> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                         [FromQuery(Name = "deviceId[]")] List<Guid> deviceIds
                                                       ) => 
            await _analyticServiceQualityService.ServiceQualityComponents(
                beg,
                end,
                applicationUserIds,
                companyIds,
                corporationIds,
                deviceIds);
        

        [HttpGet("Dashboard")]
        public string ServiceQualityDashboard([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid?> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "deviceId[]")] List<Guid> deviceIds
                                                        ) => 
            _analyticServiceQualityService.ServiceQualityDashboard(
                beg,
                end,
                applicationUserIds,
                companyIds,
                corporationIds,
                deviceIds);
        

        [HttpGet("Rating")]
        public async Task<string> ServiceQualityRating([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid?> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "deviceId[]")] List<Guid> deviceIds
                                                        ) =>
            await _analyticServiceQualityService.ServiceQualityRating(
                beg,
                end,
                applicationUserIds,
                companyIds,
                corporationIds,
                deviceIds);
        

        [HttpGet("SatisfactionStats")]
        public async Task<string> ServiceQualitySatisfactionStats([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid?> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "deviceId[]")] List<Guid> deviceIds
                                                       ) =>
            await _analyticServiceQualityService.ServiceQualitySatisfactionStats(
                beg,
                end,
                applicationUserIds,
                companyIds,
                corporationIds,
                deviceIds);
    }
}