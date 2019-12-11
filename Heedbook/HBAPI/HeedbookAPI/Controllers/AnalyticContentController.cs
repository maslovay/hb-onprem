using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using UserOperations.Utils;
using UserOperations.Providers;
using System.Threading.Tasks;
using UserOperations.Providers.Interfaces;
using System.IO;
using Microsoft.AspNetCore.Authorization;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ControllerExceptionFilter]

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
        public async Task<object> ContentShows([FromQuery(Name = "dialogueId")] Guid dialogueId)
            => await _analyticContentService.ContentShows( dialogueId);
        
        [HttpGet("Efficiency")]
        public async Task<object> Efficiency([FromQuery(Name = "begTime")] string beg,
                                                           [FromQuery(Name = "endTime")] string end,
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds) 
            => await _analyticContentService.Efficiency(
                beg, 
                end,
                applicationUserIds,
                companyIds,
                corporationIds,
                workerTypeIds);

        [HttpGet("Poll")]
        public async Task<IActionResult> Poll([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end,
                                                     [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                     [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                     [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                     [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                     [FromQuery(Name = "type")] string type = "json")
        {
            if (type != "json")
            {
                return Ok(await _analyticContentService.Poll(
                  beg,
                  end,
                  applicationUserIds,
                  companyIds,
                  corporationIds,
                  workerTypeIds));
            }
            var excelDocStream = await _analyticContentService.PollFile(
                 beg,
                 end,
                 applicationUserIds,
                 companyIds,
                 corporationIds,
                 workerTypeIds);
            return  File(excelDocStream, "application/octet-stream", "answers.xls");          
        }
    }
}

 
