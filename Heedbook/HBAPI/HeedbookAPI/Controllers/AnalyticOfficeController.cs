using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Services;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.Annotations;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ControllerExceptionFilter]
    public class AnalyticOfficeController : Controller
    {
        private readonly AnalyticOfficeService _analyticOfficeProvider;

        public AnalyticOfficeController(
            AnalyticOfficeService analyticOfficeProvider
            )
        {
            _analyticOfficeProvider = analyticOfficeProvider;
        }
        //
        [HttpGet("Efficiency")]
        [SwaggerOperation(Summary = "page: /workload",
            Description = "DialoguesNumberAvgPerEmployee - include only dialogues with UserId not null")]
        [SwaggerResponse(400, "Exception message")]
        [SwaggerResponse(200, "Key-Value")]
        public string Efficiency([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end,
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid?> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                         [FromQuery(Name = "deviceId[]")] List<Guid> deviceIds
                                                       ) =>
            _analyticOfficeProvider.Efficiency(
                beg,
                end,
                applicationUserIds,
                companyIds,
                corporationIds,
                deviceIds
            );
           
        
    }  
}