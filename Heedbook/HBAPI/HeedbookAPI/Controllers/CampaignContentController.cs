using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.Extensions.Configuration;
using HBData.Models;
using UserOperations.Services;
using Microsoft.EntityFrameworkCore;
using HBData;
using HBLib.Utils;
using Microsoft.Extensions.Primitives;
using Swashbuckle.AspNetCore.Annotations;
using UserOperations.CommonModels;
using UserOperations.Utils;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CampaignContentController : Controller
    {
        private readonly RecordsContext _context;
        private readonly IConfiguration _config;
        private readonly ILoginService _loginService;
        private readonly string _containerName;
        private Dictionary<string, string> userClaims;
        private readonly RequestFilters _requestFilters;
        // private readonly ElasticClient _log;


        public CampaignContentController(
            RecordsContext context,
            IConfiguration config,
            ILoginService loginService,
            RequestFilters requestFilters
            // ElasticClient log
            )
        {
            try
            {
            _context = context;
            _config = config;
            _loginService = loginService;
            _requestFilters = requestFilters;
            // _log = log;
            _containerName = "content-screenshots";
            //  _log.Info("Constructor of CampaignContent controller done");
            }
            catch(Exception e)
            {
                // log.Fatal($"Exception occurred {e}");
            }
        } 

        [HttpGet("Campaign")]
        [SwaggerOperation(Summary = "Return campaigns with content", Description = "Return all campaigns for loggined company with content relations")]
        [SwaggerResponse(200, "Campaigns list", typeof(List<CampaignGetModel>))]
        public IActionResult CampaignGet(
                                [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                [FromQuery(Name = "corporationIds[]")] List<Guid> corporationIds,
                                [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
            // _log.Info("Campaign get started");
            if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");  
            var role = userClaims["role"];
            var companyId = Guid.Parse(userClaims["companyId"]);     
            _requestFilters.CheckRoles(ref companyIds, corporationIds, role, companyId);  

            var statusInactiveId = _context.Statuss.FirstOrDefault(p => p.StatusName == "Inactive").StatusId;
            var campaigns = _context.Campaigns.Include(x => x.CampaignContents)
                    .Where( x => companyIds.Contains(x.CompanyId) && x.StatusId != statusInactiveId ).ToList();
            //  _log.Info("campaign get finished");
            return Ok(campaigns);
            }
            catch(Exception e)
            {
                //  _log.Fatal($"Exception occurred {e}");
                 return BadRequest("Error");
            }
        }

        [HttpPost("Campaign")]
        [SwaggerOperation(Summary = "Create campaign with content", Description = "Create new campaign with content relations and return created one")]
        [SwaggerResponse(200, "New campaign", typeof(CampaignGetModel))]
        public IActionResult CampaignPost([FromBody,
                 SwaggerParameter("Send content separately from the campaign", Required = true)] CampaignPutPostModel model, 
                 [FromHeader,  SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            // _log.Info("Campaign POST started");
            if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");             
            var companyId = Guid.Parse(userClaims["companyId"]);

            Campaign campaign = model.Campaign;
            campaign.CompanyId = (Guid)companyId;
            campaign.CreationDate = DateTime.UtcNow;
            campaign.StatusId = _context.Statuss.FirstOrDefault(p => p.StatusName == "Active").StatusId;
            campaign.CampaignContents = new List<CampaignContent>();
            _context.Add(campaign);
            _context.SaveChanges();
            foreach (var campCont in model.CampaignContents)
            {
                campCont.CampaignId = campaign.CampaignId;
                _context.Add(campCont);
            }
            _context.SaveChanges();
            // _log.Info("Campaign POST finished");
            return Ok(campaign);
        }

        [HttpPut("Campaign")]
        [SwaggerOperation(Summary = "Edit campaign with content", Description = "Edit existing campaign. Remove all content relations and create new")]
        [SwaggerResponse(200, "Edited campaign", typeof(CampaignGetModel))]

        public IActionResult CampaignPut([FromBody,
                SwaggerParameter("Send content separately from the campaign or send CampaignContents:[] if you dont need to change content relations", Required = true)] 
                CampaignPutPostModel model, [FromHeader,  SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            //  _log.Info("Campaign PUT started");
             if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");           
                Campaign modelCampaign = model.Campaign;
                try{
                var campaignEntity = _context.Campaigns.Include(x => x.CampaignContents).Where(p => p.CampaignId == modelCampaign.CampaignId).FirstOrDefault();
                if (campaignEntity == null)
                {
                    return null;
                }
                else
                {
                    if(model.CampaignContents != null && model.CampaignContents.Count != 0)
                    {
                        _context.RemoveRange(campaignEntity.CampaignContents);
                    }
                    foreach (var p in typeof(Campaign).GetProperties())
                    {
                        if (p.GetValue(modelCampaign, null) != null && p.GetValue(modelCampaign, null).ToString() != Guid.Empty.ToString())
                            p.SetValue(campaignEntity, p.GetValue(modelCampaign, null), null);
                    }
                    _context.SaveChanges();
                    foreach (var campCont in model.CampaignContents)
                    {
                        campCont.CampaignId = campaignEntity.CampaignId;
                        _context.Add(campCont);
                    }
                    _context.SaveChanges();
                }
                // _log.Info("Campaign PUT finished");
                return Ok(campaignEntity);
                }
                catch
                {
                    // _log.Fatal("Cant update");
                    return Ok("This campaign can be changed");
                }
        }

        [HttpDelete("Campaign")]
        [SwaggerOperation(Summary = "Set campaign inactive", Description = "Set campaign status Inactive and delete all content relations for this campaign")]
        public IActionResult CampaignDelete([FromQuery] Guid campaignId, [FromHeader,  SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
                // _log.Info("Campaign DELETE started");
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");  
                var campaign = _context.Campaigns.Include(x => x.CampaignContents).Where(p => p.CampaignId == campaignId).FirstOrDefault();
                if (campaign != null)
                {
                    campaign.StatusId = _context.Statuss.Where(p => p.StatusName == "Inactive").FirstOrDefault().StatusId;
                    _context.RemoveRange(campaign.CampaignContents);
                    _context.SaveChanges();
                    // _log.Info("Campaign DELETE finished");
                    return Ok("OK");
                }
                return BadRequest("No such campaign");
        }       

        [HttpGet("Content")]
        [SwaggerOperation(Summary = "Get all content", Description = "Get all content for loggined company with screenshot url links")]
        [SwaggerResponse(200, "Content list", typeof(List<Content>))]
        public async Task<IActionResult> ContentGet(
                                [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                [FromQuery(Name = "corporationIds[]")] List<Guid> corporationIds,
                                [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
            //  _log.Info("Content GET started");
            if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");             
            var role = userClaims["role"];
            var companyId = Guid.Parse(userClaims["companyId"]);     
            _requestFilters.CheckRoles(ref companyIds, corporationIds, role, companyId);  

            var contents = _context.Contents.Where(x => x.IsTemplate == true || companyIds.Contains( (Guid)x.CompanyId )).ToList();
            //  _log.Info("Content get finished");
            return Ok(contents);
            }
            catch(Exception e)
            {
                //  _log.Fatal($"Exception occurred {e}");
                 return BadRequest("Error");
            }
        }

        [HttpPost("Content")]
        [SwaggerOperation(Summary = "Save new content", Description = "Create new content")]
        [SwaggerResponse(200, "New content", typeof(Content))]
        public async Task<IActionResult> ContentPost([FromBody] Content content, [FromHeader,  SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            // _log.Info("Content POST started");
            if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");             
            var companyId = Guid.Parse(userClaims["companyId"]);
            
            if ( !content.IsTemplate ) content.CompanyId = (Guid)companyId; // only for not templates we create content for partiqular company/ Templates have no any compane relations
            content.CreationDate = DateTime.UtcNow;
            content.UpdateDate = DateTime.UtcNow;
            //content.StatusId = 3;
            // TODO: content.IsTemplate = false;
            // content.StatusId = _context.Statuss.Where(p => p.StatusName == "Active").FirstOrDefault().StatusId;;
            _context.Add(content);
            _context.SaveChanges();
            // _log.Info("Content POST finished");
            return Ok(content);
        }

        [HttpPut("Content")]
        [SwaggerOperation(Summary = "Edit content", Description = "Edit existing content")]
        [SwaggerResponse(200, "Edited content", typeof(Content))]
        public async Task<IActionResult> ContentPut(
                    [FromBody] Content content, 
                    [FromHeader,  SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            // _log.Info("Content PUT started");
            if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");  
            Content contentEntity = _context.Contents.Where(p => p.ContentId == content.ContentId).FirstOrDefault();
            foreach (var p in typeof(Content).GetProperties())
            {
                if (p.GetValue(content, null) != null && p.GetValue(content, null).ToString() != Guid.Empty.ToString())
                    p.SetValue(contentEntity, p.GetValue(content, null), null);
            }
            contentEntity.UpdateDate = DateTime.UtcNow;
            _context.SaveChanges();
            // _log.Info("Content PUT finished");
            return Ok(contentEntity);
        }

        [HttpDelete("Content")]
        [SwaggerOperation(Summary = "Remove content", Description = "Delete content")]
        public async Task<IActionResult> ContentDelete([FromQuery] Guid contentId, [FromHeader,  SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            // _log.Info("Content DELETE started");
            if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");             
            var contentEntity = _context.Contents.Include(x => x.CampaignContents).Where(p => p.ContentId == contentId).FirstOrDefault();
            if (contentEntity != null)
            {
                try
                {
                        if (contentEntity.CampaignContents != null && contentEntity.CampaignContents.Count() != 0)
                        {
                            var shownCampContentIds = _context.SlideShowSessions.Select(p => p.CampaignContentId).Distinct().ToList();
                            var campContententForRemove = contentEntity.CampaignContents
                                    .Where( x => !shownCampContentIds.Contains((Guid)x.CampaignContentId)).ToList();
                            _context.RemoveRange(campContententForRemove);
                            _context.SaveChanges();
                        }
                        if (contentEntity.CampaignContents != null && contentEntity.CampaignContents.Count() != 0)
                           {
                                //contentEntity.StatusId = 5;
                           } 
                        else
                            _context.Remove(contentEntity);
                            _context.SaveChanges();
                        // _log.Info("Content DELETE finished");
                        return Ok("OK");
                }
                catch ( Exception e )
                {
                    // _log.Fatal($"Exception occurred {e}");
                    return BadRequest(e.Message);
                }
            }
            return BadRequest("No such content");
        }
    }
}