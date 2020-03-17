using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using UserOperations.Models.Get.HomeController;
using UserOperations.Utils;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ControllerExceptionFilter]

    public class AnalyticHomeController : Controller
    {
        private readonly AnalyticHomeService _analyticHomeService;
        public AnalyticHomeController(
            AnalyticHomeService analyticHomeService
            )
        {            
            _analyticHomeService = analyticHomeService;
        }

        //[HttpGet("Dashboard")]
        //public async Task<string> GetDashboard([FromQuery(Name = "begTime")] string beg,
        //                                                [FromQuery(Name = "endTime")] string end, 
        //                                                [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
        //                                                [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
        //                                                [FromQuery(Name = "deviceId[]")] List<Guid> deviceIds) =>
        //    await _analyticHomeService.GetDashboard(
        //        beg,
        //        end,
        //        companyIds,
        //        corporationIds,
        //        deviceIds
        //    );
        

        [HttpGet("NewDashboard")]
        public async Task<NewDashboardInfo> GetNewDashboard([FromQuery(Name = "begTime")] string beg,
                                                   [FromQuery(Name = "endTime")] string end,
                                                   [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                   [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                   [FromQuery(Name = "deviceId[]")] List<Guid> deviceIds) =>
            await _analyticHomeService.GetNewDashboard(
                beg,
                end,
                companyIds,
                corporationIds,
                deviceIds);

        [HttpGet("DashboardFiltered")]
        public async Task<string> GetDashboardFiltered([FromQuery(Name = "begTime")] string beg,
                                                  [FromQuery(Name = "endTime")] string end,
                                                  [FromQuery(Name = "applicationUserId[]")] List<Guid?> applicationUserIds,
                                                  [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                  [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                  [FromQuery(Name = "deviceId[]")] List<Guid> deviceIds) =>
            await _analyticHomeService.GetDashboardFiltered(
                beg,
                end,
                applicationUserIds,
                companyIds,
                corporationIds,
                deviceIds);
    }
}