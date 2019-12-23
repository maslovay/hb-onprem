using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Services;
using Microsoft.AspNetCore.Authorization;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ControllerExceptionFilter]

    public class AnalyticReportController : Controller
    {
        private readonly AnalyticReportService _analyticReportService;

        public AnalyticReportController(
            AnalyticReportService analyticReportService
            )
        {
            _analyticReportService = analyticReportService;
        }

        [HttpGet("ActiveEmployee")]
        public string ReportActiveEmployee([FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds) => 
        _analyticReportService.ReportActiveEmployee(
            applicationUserIds,
            companyIds,
            corporationIds,
            workerTypeIds);

        [HttpGet("UserPartial")]
        public string ReportUserPartial([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end,
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds) =>
        _analyticReportService.ReportUserPartial(
            beg,
            end,
            applicationUserIds,
            companyIds,
            corporationIds,
            workerTypeIds);


        [HttpGet("UserFull")]
        public string ReportUserFull([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end,
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds ) =>
        _analyticReportService.ReportUserFull(
            beg,
            end,
            applicationUserIds,
            companyIds,
            corporationIds,
            workerTypeIds);
        
    }    
}