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
using HBData.Models;
using HBData.Models.AccountViewModels;
using UserOperations.Services;
using UserOperations.AccountModels;
using HBData;


using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Annotations;
using HBLib;
using HBLib.Utils;
using UserOperations.Utils;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhraseController : Controller
    {
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
        private readonly ElasticClient _log;
        private Dictionary<string, string> userClaims;
        private readonly RequestFilters _requestFilters;


        public PhraseController(
            ILoginService loginService,
            RecordsContext context,
            ElasticClient log,
            RequestFilters requestFilters

            )
        {
            _loginService = loginService;
            _context = context;
            _log = log;
            _requestFilters = requestFilters;

        }

        [HttpPost("PhraseScripts")]
        public IActionResult PhraseScripts([FromQuery(Name = "begTime")] string beg,
                                            [FromQuery(Name = "endTime")] string end,
                                            [FromQuery(Name = "applicationUserIds[]")] List<Guid> applicationUserIds,
                                            [FromQuery(Name = "companyIds[]")] List<Guid> companyIds,
                                            [FromQuery(Name = "corporationIds[]")] List<Guid> corporationIds,
                                            [FromQuery(Name = "workerTypeIds[]")] List<Guid> workerTypeIds,
                                            [FromQuery(Name = "phraseIds[]")] List<Guid> phraseIds,
                                            [FromHeader] string Authorization)
        {
            try
            {
                 if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);     
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRoles(ref companyIds, corporationIds, role, companyId);

                var dialogues = _context.Dialogues
                    .Include(p => p.ApplicationUser)
                    .Include(p => p.DialoguePhrase)
                    .Where(p => p.BegTime >= begTime
                            && p.EndTime <= endTime
                            && p.StatusId == 3
                            && p.InStatistic == true
                            && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                            && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                            && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)) 
                            && (!phraseIds.Any() || ! phraseIds.Except(p.DialoguePhrase.Select(r => (Guid) r.PhraseId).ToList()).Any()))
                    .Select(p => p.DialogueId).ToList();

                return Ok(dialogues);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }
    }
}
