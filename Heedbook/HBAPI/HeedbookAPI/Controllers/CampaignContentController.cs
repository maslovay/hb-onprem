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

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CampaignContentController : Controller
    {
        private readonly RecordsContext _context;
        private readonly IConfiguration _config;
        private readonly SftpClient _sftpClient;
        private readonly ILoginService _loginService;
        private readonly string _containerName;
        private Dictionary<string, string> userClaims;


        public CampaignContentController(
            RecordsContext context,
            IConfiguration config,
            SftpClient sftpClient,
            ILoginService loginService
            )
        {
            _context = context;
            _config = config;
            _sftpClient = sftpClient;
            _loginService = loginService;
            _containerName = "content-screenshots";
        }
        #region Campaign     

        [HttpGet("Campaign")]
        [SwaggerOperation(Summary = "Return campaigns with content", Description = "Return all campaigns for loggined company with content relations")]
        [SwaggerResponse(200, "Campaigns list", typeof(List<CampaignGetModel>))]
        public IActionResult CampaignGet([FromHeader,  
                SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");             
            var companyId = Guid.Parse(userClaims["companyId"]);
            var campaigns = _context.Campaigns.Include(x => x.CampaignContents).Where(x=>x.CompanyId == companyId).ToList();
            return Ok(campaigns);
        }

        [HttpPost("Campaign")]
        [SwaggerOperation(Summary = "Create campaign with content", Description = "Create new campaign with content relations and return created one")]
        [SwaggerResponse(200, "New campaign", typeof(CampaignGetModel))]
        public IActionResult CampaignPost([FromBody,
                 SwaggerParameter("Send content separately from the campaign", Required = true)] CampaignPutPostModel model, 
                 [FromHeader,  SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");             
            var companyId = Guid.Parse(userClaims["companyId"]);

            Campaign campaign = model.Campaign;
            campaign.CompanyId = (Guid)companyId;
            campaign.CreationDate = DateTime.UtcNow;
            campaign.StatusId = 2;
            campaign.CampaignContents = new List<CampaignContent>();
            _context.Add(campaign);
            _context.SaveChanges();
            foreach (var campCont in model.CampaignContents)
            {
                campCont.CampaignId = campaign.CampaignId;
                _context.Add(campCont);
            }
            _context.SaveChanges();
            return Ok(campaign);
        }

        [HttpPut("Campaign")]
        [SwaggerOperation(Summary = "Edit campaign with content", Description = "Edit existing campaign. Remove all content relations and create new")]
        [SwaggerResponse(200, "Edited campaign", typeof(CampaignGetModel))]

        public IActionResult CampaignPut([FromBody,
                SwaggerParameter("Send content separately from the campaign or send CampaignContents:[] if you dont need to change content relations", Required = true)] 
                CampaignPutPostModel model, [FromHeader,  SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
             if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");           
                Campaign modelCampaign = model.Campaign;
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
                return Ok(campaignEntity);
        }

        [HttpDelete("Campaign")]
        [SwaggerOperation(Summary = "Set campaign inactive", Description = "Set campaign status Inactive and delete all content relations for this campaign")]
        public IActionResult CampaignDelete([FromQuery] Guid campaignId, [FromHeader,  SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");  
                var campaign = _context.Campaigns.Include(x => x.CampaignContents).Where(p => p.CampaignId == campaignId).FirstOrDefault();
                if (campaign != null)
                {
                    campaign.StatusId = _context.Statuss.Where(p => p.StatusName == "Inactive").FirstOrDefault().StatusId;
                    _context.RemoveRange(campaign.CampaignContents);
                    _context.SaveChanges();
                    return Ok("OK");
                }
                return BadRequest("No such campaign");
        }       
        #endregion


        #region Content
        [HttpGet("Content")]
        [SwaggerOperation(Summary = "Get all content", Description = "Get all content for loggined company with screenshot url links")]
        [SwaggerResponse(200, "Content list", typeof(List<ContentWithScreenModel>))]
        public async Task<IActionResult> ContentGet([FromHeader,  SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");             
            var companyId = Guid.Parse(userClaims["companyId"]);
            
            var contents = _context.Contents.Where(x => x.CompanyId == companyId).ToList();
            var result = new List<ContentWithScreenModel>();
            foreach (var c in contents)
            {
                var screenshotLink = await _sftpClient.GetFileUrl(_containerName + "/" + c.ContentId.ToString() + ".png");
                result.Add(new ContentWithScreenModel(c, screenshotLink));
            }
            return Ok(result);
        }

        [HttpPost("Content")]
        [SwaggerOperation(Summary = "Save new content", Description = "Create new content and save screenshot on sftp server")]
        [SwaggerResponse(200, "New content with screenshot link", typeof(ContentWithScreenModel))]
        public async Task<IActionResult> ContentPost([FromBody] ContentWithScreenModel model, [FromHeader,  SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");             
            var companyId = Guid.Parse(userClaims["companyId"]);
            
            Content content = model.Content;
            content.CompanyId = (Guid)companyId;
            content.CreationDate = DateTime.UtcNow;
            content.UpdateDate = DateTime.UtcNow;
            //   content.StatusId = _context.Statuss.Where(p => p.StatusName == "Active").FirstOrDefault().StatusId;;
            _context.Add(content);
            _context.SaveChanges();

            string base64 = model.Screenshot;
            Byte[] imgBytes = Convert.FromBase64String(base64);
            Stream blobStream = new MemoryStream(imgBytes);
            await _sftpClient.UploadAsMemoryStreamAsync(blobStream, _containerName, content.ContentId.ToString() + ".png");
            model.Content = content;
            model.Screenshot = await _sftpClient.GetFileUrl(_containerName + "/" + content.ContentId.ToString() + ".png");
            return Ok(model);
        }

        [HttpPut("Content")]
        [SwaggerOperation(Summary = "Edit content", Description = "Edit existing content, remove screenshot from sftp and save new screenshot(if you pass it in json body)")]
        [SwaggerResponse(200, "Edited content with screenshot link", typeof(ContentWithScreenModel))]
        public async Task<IActionResult> ContentPut(
                    [FromBody] ContentWithScreenModel model, 
                    [FromHeader,  SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");         
            Content content = model.Content;
            Content contentEntity = _context.Contents.Where(p => p.ContentId == content.ContentId).FirstOrDefault();
            foreach (var p in typeof(Content).GetProperties())
            {
                if (p.GetValue(content, null) != null && p.GetValue(content, null).ToString() != Guid.Empty.ToString())
                    p.SetValue(contentEntity, p.GetValue(content, null), null);
            }
            _context.SaveChanges();
            contentEntity.UpdateDate = DateTime.UtcNow;
            if (model.Screenshot != null)
            {
                await _sftpClient.DeleteFileIfExistsAsync(_containerName +"/"+ contentEntity.ContentId.ToString() + ".png");
                string base64 = model.Screenshot;
                Byte[] imgBytes = Convert.FromBase64String(base64);
                Stream blobStream = new MemoryStream(imgBytes);
                await _sftpClient.UploadAsMemoryStreamAsync(blobStream, _containerName, content.ContentId.ToString() + ".png");
            }
            model.Content = contentEntity;
            model.Screenshot = await _sftpClient.GetFileUrl(_containerName + "/" + content.ContentId.ToString() + ".png");
            return Ok(model);
        }

        [HttpDelete("Content")]
        [SwaggerOperation(Summary = "Remove content", Description = "Delete content and remove screenshot from sftp")]
        public async Task<IActionResult> ContentDelete([FromQuery] Guid contentId, [FromHeader,  SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");             
            var contentEntity = _context.Contents.Include(x => x.CampaignContents).Where(p => p.ContentId == contentId).FirstOrDefault();
            if (contentEntity != null)
            {
                if (contentEntity.CampaignContents != null && contentEntity.CampaignContents.Count() != 0)
                {
                    _context.RemoveRange(contentEntity.CampaignContents);
                    _context.SaveChanges();
                }
                _context.Remove(contentEntity);
                _context.SaveChanges();
                await _sftpClient.DeleteFileIfExistsAsync(_containerName +"/"+ contentEntity.ContentId.ToString() + ".png");
                return Ok("OK");
            }
            return BadRequest("No such content");
        }
        #endregion
    }
}