using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using UserOperations.Services;
using Newtonsoft.Json;
using HBData;
using UserOperations.Utils;
using UserOperations.Providers;
using System.Threading.Tasks;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticHomeController : Controller
    {
        private readonly AnalyticHomeService _analyticHomeService;
        public AnalyticHomeController(
            AnalyticHomeService analyticHomeService
            )
        {            
            _analyticHomeService = analyticHomeService;
        }

        [HttpGet("Dashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization) =>
            await _analyticHomeService.GetDashboard(
                beg,
                end,
                companyIds,
                corporationIds,
                workerTypeIds,
                Authorization
            );
        

        [HttpGet("NewDashboard")]
        public async Task<IActionResult> GetNewDashboard([FromQuery(Name = "begTime")] string beg,
                                                   [FromQuery(Name = "endTime")] string end,
                                                   [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                   [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                   [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                   [FromHeader] string Authorization) =>
            await _analyticHomeService.GetNewDashboard(
                beg,
                end,
                companyIds,
                corporationIds,
                workerTypeIds,
                Authorization
            );

        [HttpGet("DashboardFiltered")]
        public async Task<IActionResult> GetDashboardFiltered([FromQuery(Name = "begTime")] string beg,
                                                  [FromQuery(Name = "endTime")] string end,
                                                  [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                  [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                  [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                  [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                  [FromHeader] string Authorization) =>
            await _analyticHomeService.GetDashboardFiltered(
                beg,
                end,
                applicationUserIds,
                companyIds,
                corporationIds,
                workerTypeIds,
                Authorization
            );
    }
}