using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using UserOperations.Services;
using UserOperations.Models.AnalyticModels;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using HBData;
using UserOperations.Utils;
using UserOperations.Providers;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticReportController : Controller
    {
        private readonly AnalyticReportService _analyticReportService;
        // private readonly ElasticClient _log;

        public AnalyticReportController(
            AnalyticReportService analyticReportService
            // ElasticClient log
            )
        {
            _analyticReportService = analyticReportService;
            // _log = log;
        }

        [HttpGet("ActiveEmployee")]
        public IActionResult ReportActiveEmployee([FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization) => 
        _analyticReportService.ReportActiveEmployee(
            applicationUserIds,
            companyIds,
            corporationIds,
            workerTypeIds,
            Authorization);

        [HttpGet("UserPartial")]
        public IActionResult ReportUserPartial([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end,
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization) =>
        _analyticReportService.ReportUserPartial(
            beg,
            end,
            applicationUserIds,
            companyIds,
            corporationIds,
            workerTypeIds,
            Authorization);


        [HttpGet("UserFull")]
        public IActionResult ReportUserFull([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end,
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization) =>
        _analyticReportService.ReportUserFull(
            beg,
            end,
            applicationUserIds,
            companyIds,
            corporationIds,
            workerTypeIds,
            Authorization);
        
    }    
}