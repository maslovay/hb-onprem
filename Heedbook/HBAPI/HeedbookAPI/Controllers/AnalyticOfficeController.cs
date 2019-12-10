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
    public class AnalyticOfficeController : Controller
    {
        private readonly AnalyticOfficeService _analyticOfficeProvider;

        public AnalyticOfficeController(
            AnalyticOfficeService analyticOfficeProvider
            )
        {
            _analyticOfficeProvider = analyticOfficeProvider;
        }

        [HttpGet("Efficiency")]
        public IActionResult Efficiency([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end,
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization) =>
            _analyticOfficeProvider.Efficiency(
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