using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Services;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Authorization;
using UserOperations.Models.Get.AnalyticSpeechController;
using UserOperations.Utils;
using HBLib.Utils;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ControllerExceptionFilter]
    public class AnalyticSpeechController : Controller
    {
        private readonly AnalyticSpeechService _analyticSpeechService;


        public AnalyticSpeechController(
            AnalyticSpeechService analyticSpeechService
            )
        {
            _analyticSpeechService = analyticSpeechService;
        }    

        [HttpGet("EmployeeRating")]
        [SwaggerOperation(Summary = "SpeechEmployeeRating", Description = "Get responce model which contains CrossFrequency, AlertFrequency for every user in current range")]
        public string SpeechEmployeeRating([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid?> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                         [FromQuery(Name = "deviceId[]")] List<Guid> deviceIds
                                                        // [FromQuery(Name = "phraseId[]")] List<Guid> phraseIds,
                                                        // [FromQuery(Name = "phraseTypeId[]")] List<Guid> phraseTypeIds
                                                        ) =>
            _analyticSpeechService.SpeechEmployeeRating(
                beg, end,
                applicationUserIds,
                companyIds,
                corporationIds,
                deviceIds);

        [HttpGet("PhraseTable")]
        [SwaggerOperation(Summary = "SpeechPhraseTable", Description = "Get responce SpeechPhrsaeTable")]
        public string SpeechPhraseTable([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid?> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                         [FromQuery(Name = "deviceId[]")] List<Guid> deviceIds,
                                                        [FromQuery(Name = "phraseId[]")] List<Guid> phraseIds,
                                                        [FromQuery(Name = "phraseTypeId[]")] List<Guid> phraseTypeIds) =>
            _analyticSpeechService.SpeechPhraseTable(
                beg, end,
                applicationUserIds,
                companyIds,
                corporationIds,
                deviceIds,
                phraseIds,
                phraseIds);
        

        [HttpGet("PhraseTypeCount")]
        [SwaggerOperation(Summary = "% phrases in dialogues", Description = "Return type, procent and colour of phrase type in dialogues (for employees, clients and total)")]
        public SpeechPhraseTotalInfo SpeechPhraseTypeCount([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid?> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                         [FromQuery(Name = "deviceId[]")] List<Guid> deviceIds,
                                                        [FromQuery(Name = "phraseId[]")] List<Guid> phraseIds,
                                                        [FromQuery(Name = "phraseTypeId[]")] List<Guid> phraseTypeIds) =>
            _analyticSpeechService.SpeechPhraseTypeCount(
                beg, end,
                applicationUserIds,
                companyIds,
                corporationIds,
                deviceIds,
                phraseIds,
                phraseTypeIds);
        

        [HttpGet("WordCloud")]
        [SwaggerOperation(Summary = "SpeechWordCloud", Description = "Get responce SpeechWordCloud")]
        public string SpeechWordCloud([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid?> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "deviceId[]")] List<Guid> deviceIds,
                                                        [FromQuery(Name = "phraseId[]")] List<Guid> phraseIds,
                                                        [FromQuery(Name = "phraseTypeId[]")] List<Guid> phraseTypeIds) =>
            _analyticSpeechService.SpeechWordCloud(
                beg, end,
                applicationUserIds,
                companyIds,
                corporationIds,
                deviceIds,
                phraseIds,
                phraseTypeIds);

        [HttpGet("PhraseSalesStageCount")]
        [SwaggerOperation(Summary = "PhraseSalesStage", Description = "If Company have corporationId, Get responce PhraseSalesStage for all companys in corporation or only for one company")]
        public string PhraseSalesStageCount([FromQuery(Name = "begTime")] string beg,
                                                      [FromQuery(Name = "endTime")] string end,
                                                      [FromQuery(Name = "corporationId")] Guid? corporationId,
                                                      [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                      [FromQuery(Name = "applicationUserId[]")] List<Guid?> applicationUserIds,
                                                      [FromQuery(Name = "deviceId[]")] List<Guid> deviceIds,
                                                      [FromQuery(Name = "phraseId[]")] List<Guid> phraseIds,
                                                      [FromQuery(Name = "salesStageId[]")] List<Guid> salesStageIds) =>
          _analyticSpeechService.PhraseSalesStageCount(
              beg, end,
              corporationId,
              companyIds,
              applicationUserIds,
              deviceIds,
              phraseIds,
              salesStageIds);
    }
}
