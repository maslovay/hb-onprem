using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using Swashbuckle.AspNetCore.Annotations;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ControllerExceptionFilter]

    public class AnalyticContentController : Controller
    {
        private readonly AnalyticContentService _analyticContentService;
        public AnalyticContentController ( AnalyticContentService analyticContentService )
        {
            _analyticContentService = analyticContentService;
        }

        [HttpGet("ContentShows")]
        [SwaggerOperation(Summary = "Data for one dialogue", Description = "Analytic about content and pool shown during dialogue")]
        [SwaggerResponse(200, "ContentInfo, AnswersInfo, AnswersAmount", typeof(Dictionary<string, object>))]
        public async Task<Dictionary<string, object>> ContentShows([FromQuery(Name = "dialogueId")] Guid dialogueId)
            => await _analyticContentService.ContentShows( dialogueId);
        

        [HttpGet("Efficiency")]
        [SwaggerOperation(Summary = "Content analytic for all dialogues", Description = "Analytic about contents shown with filters")]
        [SwaggerResponse(200, "Views, Clients, SplashViews, EmotionAttention, Age, Gender statistic for content", typeof(Dictionary<string, object>))]
        public async Task<Dictionary<string, object>> Efficiency([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end,
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid?> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "deviceId[]")] List<Guid> deviceIds) 
            => await _analyticContentService.Efficiency( beg, end, applicationUserIds, companyIds, corporationIds, deviceIds);


        [HttpGet("Poll")]
        [SwaggerOperation(Summary = "Poll analytic for all dialogues", Description = "Analytic about pools shown with filters")]
        [SwaggerResponse(200, "Views, Clients, Answers, Conversion -pool statistic for content. If type != json, return xls file", typeof(Dictionary<string, object>))]
        public async Task<IActionResult> Poll([FromQuery(Name = "begTime")] string beg,
                                                     [FromQuery(Name = "endTime")] string end,
                                                     [FromQuery(Name = "applicationUserId[]")] List<Guid?> applicationUserIds,
                                                     [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                     [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                      [FromQuery(Name = "deviceId[]")] List<Guid> deviceIds,
                                                     [FromQuery(Name = "type")] string type = "json")
        {
            var result = await _analyticContentService.Poll( beg, end, applicationUserIds, companyIds, corporationIds, deviceIds, type);
            if (type == "json")
                return Ok(result);
            return  File(result as MemoryStream, "application/octet-stream", "answers.xls");
        }
    }
}

 
