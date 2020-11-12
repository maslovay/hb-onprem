using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using HBData.Models;
using UserOperations.Services;
using Swashbuckle.AspNetCore.Annotations;
using UserOperations.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using HBLib.Utils;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ControllerExceptionFilter]
    public class CampaignContentController : Controller
    {
        private readonly CampaignContentService _campaignContentService;

        public CampaignContentController( CampaignContentService campaignContentService )
        {
            _campaignContentService = campaignContentService;
        }

        [HttpGet("Campaign")]
        [SwaggerOperation(Summary = "Return campaigns with content", Description = "Return all campaigns for loggined company with content relations. isActual= true for devices to get only active for today")]
        [SwaggerResponse(200, "Campaigns list", typeof(List<CampaignGetModel>))]
        public List<Campaign> CampaignGet(
                                [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                [FromQuery(Name = "isActual")] bool isActual = false) => 
            _campaignContentService.CampaignGet( companyIds, corporationIds, isActual);


        [HttpPost("Campaign")]
        [SwaggerOperation(Summary = "Create campaign with content", Description = "Create new campaign with content relations and return created one")]
        [SwaggerResponse(200, "New campaign", typeof(CampaignGetModel))]
        public Campaign CampaignPost(
                                [FromBody, SwaggerParameter("Send content separately from the campaign", Required = true)] 
                                CampaignPutPostModel model ) =>
            _campaignContentService.CampaignPost( model );


        [HttpPut("Campaign")]
        [SwaggerOperation(Summary = "Edit campaign with content", Description = "Edit existing campaign. Remove all content relations and create new")]
        [SwaggerResponse(200, "Edited campaign", typeof(CampaignGetModel))]
        public Campaign CampaignPut(
                                [FromBody, SwaggerParameter("Send content separately from the campaign or send CampaignContents:[] if you dont need to change content relations", Required = true)]
                                    CampaignPutPostModel model) =>
            _campaignContentService.CampaignPut( model );


        [HttpDelete("Campaign")]
        [SwaggerOperation(Summary = "Delete or set campaign inactive", Description = "Delete or set campaign status Inactive and delete all content relations for this campaign")]
        public string CampaignDelete( [FromQuery] Guid campaignId ) =>
            _campaignContentService.CampaignDelete( campaignId );


        [HttpGet("Content")]
        [SwaggerOperation(Summary = "Get all content", Description = "Get all content for loggined company with screenshot url links")]
        [SwaggerResponse(200, "Content list", typeof(List<ContentWithScreenshotModel>))]
        public async Task<object> ContentGet(
                                [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                [FromQuery(Name = "inActive")] bool inactive = false,
                                [FromQuery(Name = "screenshot")] bool screenshot = false,
                                [FromQuery(Name = "isTemplate")] bool isTemplate = false) =>
            await _campaignContentService.ContentGet( companyIds, corporationIds, inactive, screenshot, isTemplate);
        

        [HttpPost("Content")]
        [SwaggerOperation(Summary = "Save new content", Description = "Create new content")]
        [SwaggerResponse(200, "New content", typeof(ContentWithScreenshotModel))]
       
        public async Task<ContentWithScreenshotModel> ContentPost([FromForm,
                            SwaggerParameter("json content in FormData with key 'data' + file screenshot")]
                            IFormCollection formData) =>
            await _campaignContentService.ContentPost(formData);


        [HttpPut("Content")]
        [SwaggerOperation(Summary = "Edit content", Description = "Edit existing content")]
        [SwaggerResponse(200, "Edited content", typeof(ContentWithScreenshotModel))]
        public async Task<ContentWithScreenshotModel> ContentPut([FromForm,
                            SwaggerParameter("json content in FormData with key 'data' + file screenshot")]
                            IFormCollection formData) =>
            await _campaignContentService.ContentPut(formData);


        [HttpDelete("Content")]
        [SwaggerOperation(Summary = "Remove content", Description = "Delete content")]
        public async Task<string> ContentDelete( [FromQuery] Guid contentId ) =>
            await _campaignContentService.ContentDelete( contentId );


        [HttpGet("GetResponseHeaders")]
        [SwaggerOperation(Summary = "GetResponceheader", Description = "Method return Url responce headers")]
        [AllowAnonymous]
        public async Task<Dictionary<string, string>> GetResponseHeaders([FromQuery] string url) =>
            await _campaignContentService.GetResponseHeaders(url);
    }
}