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
using UserOperations.Providers;

namespace UserOperations.Services
{
    [Route("api/[controller]")]
    [ApiController]
    public class CampaignContentService : Controller
    {
        private readonly ILoginService _loginService;
        private Dictionary<string, string> userClaims;
        private readonly IRequestFilters _requestFilters;
        private readonly ICampaignContentProvider _campaignContentProvider;

        public CampaignContentService(
            ILoginService loginService,
            IRequestFilters requestFilters,
            ICampaignContentProvider campaignContentProvider
            )
        {
            try
            {
                _loginService = loginService;
                _requestFilters = requestFilters;
                _campaignContentProvider = campaignContentProvider;
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
                                [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);

                var statusInactiveId =  _campaignContentProvider.GetStatusId("Inactive");
                var campaigns = _campaignContentProvider.GetCampaignForCompanys(companyIds, statusInactiveId);
                
                var result = campaigns
                    .Select(p => 
                        {
                            p.CampaignContents = p.CampaignContents.Where(x => p.CampaignContents != null
                                    && p.CampaignContents.Count != 0
                                    && x.StatusId != statusInactiveId)
                                .ToList();
                            return p;
                        })
                    .ToList();
                return Ok(result);
            }
            catch (Exception e)
            {
                return BadRequest("Error");
            }
        }

        [HttpPost("Campaign")]
        [SwaggerOperation(Summary = "Create campaign with content", Description = "Create new campaign with content relations and return created one")]
        [SwaggerResponse(200, "New campaign", typeof(CampaignGetModel))]
        public IActionResult CampaignPost(
                [FromBody, SwaggerParameter("Send content separately from the campaign", Required = true)] CampaignPutPostModel model,
                [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            // _log.Info("Campaign POST started");
            if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                return BadRequest("Token wrong");
            var companyId = Guid.Parse(userClaims["companyId"]);
            var activeStatus = _campaignContentProvider.GetStatusId("Active");
            Campaign campaign = model.Campaign;
            campaign.CompanyId = (Guid)companyId;
            campaign.CreationDate = DateTime.UtcNow;
            campaign.StatusId = activeStatus;
            campaign.CampaignContents = new List<CampaignContent>();
            _campaignContentProvider.AddInBase<Campaign>(campaign);
            _campaignContentProvider.SaveChanges();
            foreach (var campCont in model.CampaignContents)
            {
                campCont.CampaignId = campaign.CampaignId;
                campCont.StatusId = activeStatus;
                _campaignContentProvider.AddInBase<CampaignContent>(campCont);
            }
            _campaignContentProvider.SaveChanges();
            // _log.Info("Campaign POST finished");
            return Ok(campaign);
        }

        [HttpPut("Campaign")]
        [SwaggerOperation(Summary = "Edit campaign with content", Description = "Edit existing campaign. Remove all content relations and create new")]
        [SwaggerResponse(200, "Edited campaign", typeof(CampaignGetModel))]

        public IActionResult CampaignPut(
                [FromBody, SwaggerParameter("Send content separately from the campaign or send CampaignContents:[] if you dont need to change content relations", Required = true)]
                    CampaignPutPostModel model, 
                [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            if (!_loginService.GetDataFromToken(Authorization, out userClaims)) return BadRequest("Token wrong");
            Guid.TryParse(userClaims["companyId"], out var companyIdInToken);
            Guid.TryParse(userClaims["corporationId"], out var corporationIdInToken);
            var roleInToken = userClaims["role"];

            Campaign modelCampaign = model.Campaign;
            try
            {
                var campaignEntity = _campaignContentProvider.GetCampaign(modelCampaign.CampaignId);
                if (campaignEntity == null) return null;
                if (_requestFilters.IsCompanyBelongToUser(corporationIdInToken, companyIdInToken, campaignEntity.CompanyId, roleInToken) == false)
                    return BadRequest($"Not allowed user company");

                foreach (var p in typeof(Campaign).GetProperties())
                {
                    if (p.GetValue(modelCampaign, null) != null && p.GetValue(modelCampaign, null).ToString() != Guid.Empty.ToString())
                        p.SetValue(campaignEntity, p.GetValue(modelCampaign, null), null);
                }

                var inactiveStatusId = _campaignContentProvider.GetStatusId("Inactive");
                var activeStatusId = _campaignContentProvider.GetStatusId("Active");
                var activeCampaignContents = campaignEntity.CampaignContents.Where(x => x.StatusId != inactiveStatusId).ToList();

                if (model.CampaignContents != null && model.CampaignContents.Count != 0)
                {
                    // foreach (var item in activeCampaignContents)
                    // {
                    //     if (!model.CampaignContents.Select(x => x.CampaignContentId).Contains(item.CampaignContentId))
                    //     {
                    //         item.StatusId = inactiveStatusId;
                    //     }
                    // }
                    var modelCampaignComtentIds = model.CampaignContents.Select(x => x.CampaignContentId);
                    activeCampaignContents.Where(p => !modelCampaignComtentIds.Contains(p.CampaignContentId))
                        .Select(p => 
                            {
                                p.StatusId = inactiveStatusId;
                                return p;
                            })
                        .ToList();
                    // foreach (var campCont in model.CampaignContents)
                    // {
                    //     if (!activeCampaignContents.Select(x => x.CampaignContentId).Contains(campCont.CampaignContentId))
                    //     {
                    //         campCont.CampaignId = campaignEntity.CampaignId;
                    //         campCont.StatusId = activeStatusId;
                    //         _campaignContentProvider.AddInBase<CampaignContent>(campCont);
                    //     }
                    // }
                    var activeCampaignContentsIds = activeCampaignContents.Select(x => x.CampaignContentId);
                    model.CampaignContents.Where(p => !activeCampaignContentsIds.Contains(p.CampaignContentId))
                        .Select(p =>
                            {
                                p.CampaignId = campaignEntity.CampaignId;
                                p.StatusId = activeStatusId;
                                _campaignContentProvider.AddInBase<CampaignContent>(p);
                                return p;
                            })
                        .ToList();
                }
                _campaignContentProvider.SaveChanges();
                campaignEntity.CampaignContents = campaignEntity.CampaignContents.Where(x => x.StatusId != inactiveStatusId).ToList();
                return Ok(campaignEntity);
            }
            catch (Exception e)
            {
                // _log.Fatal("Cant update");
                return BadRequest(e.Message);
            }
        }

        [HttpDelete("Campaign")]
        [SwaggerOperation(Summary = "Delete or set campaign inactive", Description = "Delete or set campaign status Inactive and delete all content relations for this campaign")]
        public IActionResult CampaignDelete([FromQuery] Guid campaignId, [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                return BadRequest("Token wrong");

            Guid.TryParse(userClaims["companyId"], out var companyIdInToken);
            Guid.TryParse(userClaims["corporationId"], out var corporationIdInToken);
            var roleInToken = userClaims["role"];

            var campaign = _campaignContentProvider.GetCampaign(campaignId);
            if (_requestFilters.IsCompanyBelongToUser(corporationIdInToken, companyIdInToken, campaign.CompanyId, roleInToken) == false)
                return BadRequest($"Not allowed user company");
            if (campaign != null)
            {
                var inactiveStatusId = _campaignContentProvider.GetStatusId("Inactive");
                var links = campaign.CampaignContents.ToList();
                foreach (var campaignContent in links)
                {
                    campaignContent.StatusId = inactiveStatusId;
                }
                campaign.StatusId = inactiveStatusId;
                _campaignContentProvider.SaveChanges();
                try
                {
                    _campaignContentProvider.RemoveRange<CampaignContent>(campaign.CampaignContents);
                    _campaignContentProvider.RemoveEntity<Campaign>(campaign);
                    _campaignContentProvider.SaveChanges();
                }
                catch
                {
                    return Ok("Set inactive");
                }
                return Ok("Deleted");
            }
            return BadRequest("No such campaign");
        }

        [HttpGet("Content")]
        [SwaggerOperation(Summary = "Get all content", Description = "Get all content for loggined company with screenshot url links")]
        [SwaggerResponse(200, "Content list", typeof(List<Content>))]
        public async Task<IActionResult> ContentGet(
                                [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                [FromQuery(Name = "inActive")] bool? inActive,
                                [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);

                var activeStatusId = _campaignContentProvider.GetStatusId("Active");
                List<Content> contents;
                if (inActive == true)
                    contents = _campaignContentProvider.GetContentsWithActiveStatusId(activeStatusId, companyIds);
                else
                    contents = _campaignContentProvider.GetContentsWithTemplateIsTrue(companyIds);
                return Ok(contents);
            }
            catch (Exception e)
            {
                return BadRequest("Error");
            }
        }
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
                                [FromQuery(Name = "orderDirection")] string orderDirection = "desc")
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);

                var activeStatusId = _campaignContentProvider.GetStatusId("Active");
                List<Content> contents;
                if(inActive == true)
                    contents = _campaignContentProvider.GetContentsWithActiveStatusId(activeStatusId, companyIds);
                else
                    contents = _campaignContentProvider.GetContentsWithTemplateIsTrue(companyIds);

                if(contents.Count == 0) return Ok(contents);

                ////---PAGINATION---
                var pageCount = (int)Math.Ceiling((double)contents.Count() / limit);//---round to the bigger 

                Type contentType = contents.First().GetType();
                PropertyInfo prop = contentType.GetProperty(orderBy);
                
                if (orderDirection == "asc")
                {
                    var contentsList = contents.OrderBy(p => prop.GetValue(p)).Skip(page * limit).Take(limit).ToList();
                    return Ok(new { contentsList, pageCount, orderBy, limit, page });                    
                }
                else
                {
                    var contentsList = contents.OrderByDescending(p => prop.GetValue(p)).Skip(page * limit).Take(limit).ToList();
                    return Ok(new { contentsList, pageCount, orderBy, limit, page });                    
                }
            }
            catch (Exception e)
            {
                return BadRequest("Error");
            }
        }

        [HttpPost("Content")]
        [SwaggerOperation(Summary = "Save new content", Description = "Create new content")]
        [SwaggerResponse(200, "New content", typeof(Content))]
        public async Task<IActionResult> ContentPost([FromBody] Content content, [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                return BadRequest("Token wrong");
            Guid.TryParse(userClaims["companyId"], out var companyIdInToken);
            var roleInToken = userClaims["role"];

            if (!content.IsTemplate) content.CompanyId = companyIdInToken; // only for not templates we create content for partiqular company/ Templates have no any compane relations
            content.CreationDate = DateTime.UtcNow;
            content.UpdateDate = DateTime.UtcNow;
            content.StatusId = _campaignContentProvider.GetStatusId("Active");
            _campaignContentProvider.AddInBase<Content>(content);
            _campaignContentProvider.SaveChanges();
            return Ok(content);
        }

        [HttpPut("Content")]
        [SwaggerOperation(Summary = "Edit content", Description = "Edit existing content")]
        [SwaggerResponse(200, "Edited content", typeof(Content))]
        public async Task<IActionResult> ContentPut(
                    [FromBody] Content content,
                    [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                return BadRequest("Token wrong");

            Guid.TryParse(userClaims["companyId"], out var companyIdInToken);
            Guid.TryParse(userClaims["corporationId"], out var corporationIdInToken);
            var roleInToken = userClaims["role"];

            if (_requestFilters.IsCompanyBelongToUser(corporationIdInToken, companyIdInToken, content.CompanyId, roleInToken) == false)
                return BadRequest($"Not allowed user company");

            var contentEntity = _campaignContentProvider.GetContent(content.ContentId);
            foreach (var p in typeof(Content).GetProperties())
            {
                if (p.GetValue(content, null) != null && p.GetValue(content, null).ToString() != Guid.Empty.ToString())
                    p.SetValue(contentEntity, p.GetValue(content, null), null);
            }
            contentEntity.UpdateDate = DateTime.UtcNow;
            await _campaignContentProvider.SaveChangesAsync();
            return Ok(contentEntity);
        }

        [HttpDelete("Content")]
        [SwaggerOperation(Summary = "Remove content", Description = "Delete content")]
        public async Task<IActionResult> ContentDelete([FromQuery] Guid contentId, [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            // _log.Info("Content DELETE started");
            if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                return BadRequest("Token wrong");

            Guid.TryParse(userClaims["companyId"], out var companyIdInToken);
            Guid.TryParse(userClaims["corporationId"], out var corporationIdInToken);
            var roleInToken = userClaims["role"];

            var content = _campaignContentProvider.GetContentWithIncludeCampaignContent(contentId);
            if (content == null) return BadRequest("No such content");
            if (_requestFilters.IsCompanyBelongToUser(corporationIdInToken, companyIdInToken, content.CompanyId, roleInToken) == false)
                return BadRequest($"Not allowed user company");

            var inactiveStatusId = _campaignContentProvider.GetStatusId("Inactive");
            var links = content.CampaignContents.ToList();
            if (links.Count() != 0)
            {
                foreach (var campaignContent in links)
                {
                    campaignContent.StatusId = inactiveStatusId;
                }
            }
            content.StatusId = inactiveStatusId;
            _campaignContentProvider.SaveChanges();
            try
            {
                _campaignContentProvider.RemoveRange<CampaignContent>(links);
                _campaignContentProvider.RemoveEntity(content);
                _campaignContentProvider.SaveChanges();
            }
            catch
            {
                return BadRequest("Set inactive");
            }
            return Ok("Removed");
        }

        [HttpGet("GetResponseHeaders")]
        public async Task<IActionResult> GetResponseHeaders([FromQuery] string url)
        {
            try
            {
                var MyClient = WebRequest.Create(url) as HttpWebRequest;
                MyClient.Method = WebRequestMethods.Http.Get;
                MyClient.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                var response = (await MyClient.GetResponseAsync()) as HttpWebResponse;
                var answer = new Dictionary<string, string>();
                for (int i = 0; i < response.Headers.Count; i++)
                    answer[response.Headers.GetKey(i)] = response.Headers.Get(i).ToString();
                return Ok(answer);
            }
            catch
            {
                return BadRequest("Error");
            }
        }
    }
}