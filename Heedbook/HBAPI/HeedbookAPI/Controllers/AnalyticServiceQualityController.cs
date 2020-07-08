using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using HBLib.Utils;
using Swashbuckle.AspNetCore.Annotations;

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
        [SwaggerOperation(Summary = "Get ServiceQualityComponents", Description = "Get responce ComponentsSatisfactionInfo model whitch contains average properties for current period")]
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
        [SwaggerOperation(Summary = "Get ServiceQualityDashboard", Description = "Get responce ComponentsDashboardInfo model")]
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
        [SwaggerOperation(Summary = "", Description = "Get responce RatingRatingInfo model, which contains SatisfactionIndex, DialogueCount, PositiveEmotionShare, AttentionShare, PositiveToneShare, TextLoyaltyShare, TextPositiveShare.")]
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
        [SwaggerOperation(Summary = "ServiceQualitySatisfactionStats", Description = "Get responce SatisfactionStatsInfo model whitch contains AverageSatisfactionScore, PeriodSatisfaction")]
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