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
    public class AnalyticRatingController : Controller
    {
        private readonly AnalyticRatingService _analyticRatingService;

        public AnalyticRatingController(
            AnalyticRatingService analyticRatingService
            )
        {
            _analyticRatingService = analyticRatingService;
        }

        [HttpGet("Progress")]
        public async Task<string> RatingProgress([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid?> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                         [FromQuery(Name = "deviceId[]")] List<Guid> deviceIds) =>
            await _analyticRatingService.RatingProgress(
                beg, end,
                applicationUserIds, companyIds, corporationIds, deviceIds
            );
        


        [HttpGet("RatingUsers")]
        public async Task<string> RatingUsers([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid?> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "deviceId[]")] List<Guid> deviceIds) =>
            await _analyticRatingService.RatingUsers(
                beg, end,
                applicationUserIds, companyIds, corporationIds, deviceIds
            );
        


        [HttpGet("RatingOffices")]
        public async Task<string> RatingOffices([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "deviceId[]")] List<Guid> deviceIds) =>
            await _analyticRatingService.RatingOffices(
                beg, end,
                companyIds, corporationIds, deviceIds
            );
                 
    }
}