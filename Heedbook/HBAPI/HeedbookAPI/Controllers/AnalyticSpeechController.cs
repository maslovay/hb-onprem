using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;
using UserOperations.AccountModels;
using HBData.Models;
using HBData.Models.AccountViewModels;
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
using Swashbuckle.AspNetCore.Annotations;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
        public IActionResult SpeechEmployeeRating([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        // [FromQuery(Name = "phraseId[]")] List<Guid> phraseIds,
                                                        // [FromQuery(Name = "phraseTypeId[]")] List<Guid> phraseTypeIds,
                                                        [FromHeader] string Authorization) =>
            _analyticSpeechService.SpeechEmployeeRating(
                beg,
                end,
                applicationUserIds,
                companyIds,
                corporationIds,
                workerTypeIds,
                Authorization);

        [HttpGet("PhraseTable")]
        public IActionResult SpeechPhraseTable([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromQuery(Name = "phraseId[]")] List<Guid> phraseIds,
                                                        [FromQuery(Name = "phraseTypeId[]")] List<Guid> phraseTypeIds,
                                                        [FromHeader] string Authorization) =>
            _analyticSpeechService.SpeechPhraseTable(
                beg,
                end,
                applicationUserIds,
                companyIds,
                corporationIds,
                workerTypeIds,
                phraseIds,
                phraseIds,
                Authorization);
        

        [HttpGet("PhraseTypeCount")]
        [SwaggerOperation(Summary = "% phrases in dialogues", Description = "Return type, procent and colour of phrase type in dialogues (for employees, clients and total)")]
        public IActionResult SpeechPhraseTypeCount([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromQuery(Name = "phraseId[]")] List<Guid> phraseIds,
                                                        [FromQuery(Name = "phraseTypeId[]")] List<Guid> phraseTypeIds,
                                                        [FromHeader] string Authorization) =>
            _analyticSpeechService.SpeechPhraseTypeCount(
                beg,
                end,
                applicationUserIds,
                companyIds,
                corporationIds,
                workerTypeIds,
                phraseIds,
                phraseTypeIds,
                Authorization);
        

        [HttpGet("WordCloud")]
        public IActionResult SpeechWordCloud([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromQuery(Name = "phraseId[]")] List<Guid> phraseIds,
                                                        [FromQuery(Name = "phraseTypeId[]")] List<Guid> phraseTypeIds,
                                                        [FromHeader] string Authorization) =>
            _analyticSpeechService.SpeechWordCloud(
                beg,
                end,
                applicationUserIds,
                companyIds,
                corporationIds,
                workerTypeIds,
                phraseIds,
                phraseTypeIds,
                Authorization);
        
    }    
}
