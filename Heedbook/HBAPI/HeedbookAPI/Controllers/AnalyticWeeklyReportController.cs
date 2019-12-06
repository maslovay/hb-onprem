using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using UserOperations.Services;
using UserOperations.Models.AnalyticModels;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using HBData;
using UserOperations.Utils;
using HBLib.Utils;
using HBData.Models;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
        public IActionResult User(
                [FromHeader] string Authorization,
                [FromQuery(Name = "applicationUserId")] Guid userId) =>
            _analyticWeeklyReportService.User(Authorization, userId);
        
    }
}