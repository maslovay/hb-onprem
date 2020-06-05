using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Services;
using Microsoft.AspNetCore.Authorization;
using UserOperations.Utils;
using HBLib.Utils;
using Swashbuckle.AspNetCore.Annotations;

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
        [SwaggerOperation(Summary = "ActiveEmployee", Description = "Get Employee SessionInfo List according parameters")]
        public string ReportActiveEmployee([FromQuery(Name = "applicationUserId[]")] List<Guid?> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                         [FromQuery(Name = "deviceId[]")] List<Guid> deviceIds) => 
        _analyticReportService.ReportActiveEmployee(
            applicationUserIds,
            companyIds,
            corporationIds,
            deviceIds);

        [HttpGet("UserPartial")]
        [SwaggerOperation(Summary = "Get ReportUserPartial", Description = "Receive list of ReportPartDayEmployeeInfo which contains number of dialogues, working hours, load index for every day in range of days.")]
        public string ReportUserPartial([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end,
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid?> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "deviceId[]")] List<Guid> deviceIds) =>
        _analyticReportService.ReportUserPartial(
            beg,
            end,
            applicationUserIds,
            companyIds,
            corporationIds,
            deviceIds);


        [HttpGet("UserFull")]
        [SwaggerOperation(Summary = "Get ReportUserFull", Description = "Receive list of ReportFullPeriodInfo")]
        public string ReportUserFull([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end,
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid?> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "deviceId[]")] List<Guid> deviceIds) =>
        _analyticReportService.ReportUserFull(
            beg,
            end,
            applicationUserIds,
            companyIds,
            corporationIds,
            deviceIds);
        
    }    
}