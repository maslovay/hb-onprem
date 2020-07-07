using System;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Services;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using HBLib.Utils;
using Swashbuckle.AspNetCore.Annotations;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ControllerExceptionFilter]

    //---for Heedbook\HBOperations\QuartzExtensions\utils\WeeklyReport\WeeklyReport.cs
    public class AnalyticWeeklyReportController : Controller
    {
        private readonly AnalyticWeeklyReportService _analyticWeeklyReportService;

        public AnalyticWeeklyReportController(
            AnalyticWeeklyReportService analyticWeeklyReportService
            )
        {
            _analyticWeeklyReportService = analyticWeeklyReportService;
        }

        [HttpGet("User")]
        [SwaggerOperation(Summary = "UserReport", Description = "Responce user weekly report")]
        public Dictionary<string, object> User(
                [FromQuery(Name = "applicationUserId")] Guid userId,
                [FromQuery(Name = "begTime")] string beg,
                [FromQuery(Name = "endTime")] string end) => 
            _analyticWeeklyReportService.User(userId, beg, end);
        
    }
}