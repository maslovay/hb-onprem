using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Services;
using UserOperations.Models.AnalyticModels;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using UserOperations.Utils;
using UserOperations.Providers;
using System.Threading.Tasks;
using UserOperations.Providers.Interfaces;
using System.IO;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticContentController : Controller
    {
        private readonly AnalyticContentService _analyticContentService;
        public AnalyticContentController(
            AnalyticContentService analyticContentService
            )
        {
            _analyticContentService = analyticContentService;
        }

//---FOR ONE DIALOGUE---
        [HttpGet("ContentShows")]
        public async Task<IActionResult> ContentShows([FromQuery(Name = "dialogueId")] Guid dialogueId,
                                                        [FromHeader] string Authorization) => 
            await _analyticContentService.ContentShows(
                dialogueId, 
                Authorization);
        
        [HttpGet("Efficiency")]
        public async Task<IActionResult> Efficiency([FromQuery(Name = "begTime")] string beg,
                                                           [FromQuery(Name = "endTime")] string end,
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization) => 
            await _analyticContentService.Efficiency(
                beg, 
                end,
                applicationUserIds,
                companyIds,
                corporationIds,
                workerTypeIds,
                Authorization);

        [HttpGet("Poll")]
        public async Task<IActionResult> Poll([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end,
                                                     [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                     [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                     [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                     [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                     [FromHeader] string Authorization,
                                                     [FromQuery(Name = "type")] string type = "json"
                                                     ) => 
            await _analyticContentService.Poll(
                beg,
                end,
                applicationUserIds,
                companyIds,
                corporationIds,
                workerTypeIds,
                Authorization,
                type);
    }
}

 
