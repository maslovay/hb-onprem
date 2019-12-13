using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using HBData.Models;
using UserOperations.Services;
using Microsoft.EntityFrameworkCore;
using HBData;
using Swashbuckle.AspNetCore.Annotations;
using UserOperations.CommonModels;
using UserOperations.Utils;
using Newtonsoft.Json;
using System.Reflection;
using System.Net;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CampaignContentController : Controller
    {
        private readonly RecordsContext _context;
        private readonly IConfiguration _config;
        private readonly LoginService _loginService;
        private Dictionary<string, string> userClaims;
        private readonly RequestFilters _requestFilters;
        private readonly CampaignContentService _campaignContentService;


        public CampaignContentController(
            RecordsContext context,
            IConfiguration config,
            LoginService loginService,
            RequestFilters requestFilters,
            CampaignContentService campaignContentService
            )
        {
            try
            {
                _context = context;
                _config = config;
                _loginService = loginService;
                _requestFilters = requestFilters;
                _campaignContentService = campaignContentService;
            }
            catch (Exception e)
            {
            }
        }

        [HttpGet("Campaign")]
        [SwaggerOperation(Summary = "Return campaigns with content", Description = "Return all campaigns for loggined company with content relations")]
        [SwaggerResponse(200, "Campaigns list", typeof(List<CampaignGetModel>))]
        public IActionResult CampaignGet(
                                [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization) => 
            _campaignContentService.CampaignGet(
                companyIds,
                corporationIds,
                Authorization);

        [HttpPost("Campaign")]
        [SwaggerOperation(Summary = "Create campaign with content", Description = "Create new campaign with content relations and return created one")]
        [SwaggerResponse(200, "New campaign", typeof(CampaignGetModel))]
        public IActionResult CampaignPost(
                                [FromBody, SwaggerParameter("Send content separately from the campaign", Required = true)] 
                                    CampaignPutPostModel model,
                                [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization) =>
            _campaignContentService.CampaignPost(
                model,
                Authorization);

        [HttpPut("Campaign")]
        [SwaggerOperation(Summary = "Edit campaign with content", Description = "Edit existing campaign. Remove all content relations and create new")]
        [SwaggerResponse(200, "Edited campaign", typeof(CampaignGetModel))]

        public IActionResult CampaignPut(
                                [FromBody, SwaggerParameter("Send content separately from the campaign or send CampaignContents:[] if you dont need to change content relations", Required = true)]
                                    CampaignPutPostModel model, 
                                [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization) =>
            _campaignContentService.CampaignPut(
                model,
                Authorization);

        [HttpDelete("Campaign")]
        [SwaggerOperation(Summary = "Delete or set campaign inactive", Description = "Delete or set campaign status Inactive and delete all content relations for this campaign")]
        public IActionResult CampaignDelete(
                                [FromQuery] Guid campaignId, 
                                [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization) =>
            _campaignContentService.CampaignDelete(
                campaignId,
                Authorization);

        [HttpGet("Content")]
        [SwaggerOperation(Summary = "Get all content", Description = "Get all content for loggined company with screenshot url links")]
        [SwaggerResponse(200, "Content list", typeof(List<Content>))]
        public async Task<IActionResult> ContentGet(
                                [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                [FromQuery(Name = "inActive")] bool? inActive,
                                [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization) =>
            await _campaignContentService.ContentGet(
                companyIds,
                corporationIds,
                inActive,
                Authorization);
        
        [HttpGet("ContentPaginated")]
        [SwaggerOperation(Summary = "Get all content", Description = "Return content for loggined company with screenshot url links (one page). limit=10, page=0, orderBy=Name/CreationDate/UpdateDate, orderDirection=desc/asc")]
        [SwaggerResponse(200, "Content list", typeof(List<Content>))]
        public async Task<IActionResult> ContentPaginatedGet(
                                [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                [FromQuery(Name = "inActive")] bool? inActive,
                                [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization,
                                [FromQuery(Name = "limit")] int limit = 10,
                                [FromQuery(Name = "page")] int page = 0,
                                [FromQuery(Name = "orderBy")] string orderBy = "Name",
                                [FromQuery(Name = "orderDirection")] string orderDirection = "desc") =>
            await _campaignContentService.ContentPaginatedGet(
                companyIds,
                corporationIds,
                inActive,
                Authorization,
                limit,
                page,
                orderBy,
                orderDirection);

        [HttpPost("Content")]
        [SwaggerOperation(Summary = "Save new content", Description = "Create new content")]
        [SwaggerResponse(200, "New content", typeof(Content))]
        public async Task<IActionResult> ContentPost(
                                [FromBody] Content content, 
                                [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization) =>
            await _campaignContentService.ContentPost(
                content,
                Authorization);

        [HttpPut("Content")]
        [SwaggerOperation(Summary = "Edit content", Description = "Edit existing content")]
        [SwaggerResponse(200, "Edited content", typeof(Content))]
        public async Task<IActionResult> ContentPut(
                                [FromBody] Content content,
                                [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization) =>
            await _campaignContentService.ContentPut(
                content,
                Authorization);

        [HttpDelete("Content")]
        [SwaggerOperation(Summary = "Remove content", Description = "Delete content")]
        public async Task<IActionResult> ContentDelete(
                                [FromQuery] Guid contentId, 
                                [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization) =>
            await _campaignContentService.ContentDelete(
                contentId,
                Authorization);

        [HttpGet("GetResponseHeaders")]
        public async Task<IActionResult> GetResponseHeaders([FromQuery] string url) =>
            await _campaignContentService.GetResponseHeaders(url);
    }
}