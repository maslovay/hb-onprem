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

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticContentController : Controller
    {
        private readonly IConfiguration _config;        
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
        private readonly DBOperations _dbOperation;
        private readonly RequestFilters _requestFilters;
        private readonly ElasticClient _log;


        public AnalyticContentController(
            IConfiguration config,
            ILoginService loginService,
            RecordsContext context,
            DBOperations dbOperation,
            RequestFilters requestFilters,
            ElasticClient log
            )
        {
            _config = config;
            _loginService = loginService;
            _context = context;
            _dbOperation = dbOperation;
            _requestFilters = requestFilters;
            _log = log;
        }

        [HttpGet("ContentShows")]
        public IActionResult ContentShows([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationIds[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                _log.Info("AnalyticSpeech/CrossRating started");
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);     
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRoles(ref companyIds, corporationIds, role, companyId);       

                var typeIdCross = _context.PhraseTypes.Where(p => p.PhraseTypeText == "Cross").Select(p => p.PhraseTypeId).First();
                //Dialogues info
                var dialogues = _context.Dialogues
                    .Include(p => p.ApplicationUser)
                    .Include(p => p.DialogueClientSatisfaction)
                    .Include(p => p.DialoguePhraseCount)
                    .Where(p => p.BegTime >= begTime
                            && p.EndTime <= endTime
                            && p.StatusId == 3
                            && p.InStatistic == true
                            && (!companyIds.Any() || companyIds.Contains((Guid) p.ApplicationUser.CompanyId))
                            && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                            && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)))
                    .Select(p => new DialogueInfo
                    {
                        DialogueId = p.DialogueId,
                        ApplicationUserId = p.ApplicationUserId,
                        FullName = p.ApplicationUser.FullName,
                        CrossCout = p.DialoguePhraseCount.Where(q => q.PhraseTypeId == typeIdCross).Count(),
                    })
                    .ToList();
                //Result
                var result = dialogues
                    .GroupBy(p => p.ApplicationUserId)
                    .Select(p => new
                    {
                        FullName = p.First().FullName,
                        CrossIndex = p.Any() ? (double?) p.Average(q => Math.Min(q.CrossCout, 1)) : null
                    }).ToList();
                result = result.OrderBy(p => p.CrossIndex).ToList();
                var jsonToReturn = JsonConvert.SerializeObject(result);
                _log.Info("AnalyticSpeech/CrossRating finished");
                return Ok(jsonToReturn);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        
        }}
       

    public class ContentTotalInfo
    {
        public List<SpeechPhrasesInfo> Client { get; set; }
        public List<SpeechPhrasesInfo> Employee { get; set; }
        public List<SpeechPhrasesInfo> Total{ get; set; }
    }
}
