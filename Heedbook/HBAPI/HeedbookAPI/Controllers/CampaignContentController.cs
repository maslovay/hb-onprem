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



namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CampaignContentController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
        private readonly SftpClient _sftpClient;
        private readonly string _containerName;
        private Dictionary<string, string> userClaims;


        public CampaignContentController(
            IConfiguration config,
            ILoginService loginService,
            RecordsContext context,
            SftpClient sftpClient
            )
        {
            _config = config;
            _loginService = loginService;
            _context = context;
            _sftpClient = sftpClient;
            _containerName = "content-screenshots";
        }
        #region Campaign
          [HttpGet("Test")]
        [SwaggerOperation(Description = "Return all camapigns for loggined company with content relations")]
        public IActionResult Test()
        {
           
            return Ok("Its working");
        }

        [HttpGet("Campaign")]
        [SwaggerOperation(Description = "Return all camapigns for loggined company with content relations")]
        public IActionResult CampaignGet([FromHeader] string Authorization)
        {
            if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");             
            var companyId = Guid.Parse(userClaims["companyId"]);
            var campaigns = _context.Campaigns.Include(x => x.CampaignContents).Where(x=>x.CompanyId == companyId).ToList();
            return Ok(campaigns);
        }

        [HttpPost("Campaign")]
        [SwaggerOperation(Description = "Create new campaign with content relations")]
        public IActionResult CampaignPost([FromBody] CampaignModel model, [FromHeader] string Authorization)
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
        [SwaggerOperation(Description = "Edit existing campaign. Remove all content relations and create new")]
        public IActionResult CampaignPut([FromBody] CampaignModel model, [FromHeader] string Authorization)
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
                    foreach (var campCont in campaignEntity.CampaignContents)
                    {
                        _context.Remove(campCont);
                    }
                    foreach (var p in typeof(Campaign).GetProperties())
                    {
                        if (p.GetValue(modelCampaign, null) != null)
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
        [SwaggerOperation(Description = "Set camapign status Inactive and delete all content relations for this campaign")]
        public IActionResult CampaignDelete([FromQuery] Guid campaignId, [FromHeader] string Authorization)
        {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");  
                var campaign = _context.Campaigns.Include(x => x.CampaignContents).Where(p => p.CampaignId == campaignId).FirstOrDefault();
                if (campaign != null)
                {
                    campaign.StatusId = _context.Statuss.Where(p => p.StatusName == "Inactive").FirstOrDefault().StatusId;
                    foreach (var item in campaign.CampaignContents)
                    {
                        _context.Remove(item);
                    }
                    _context.SaveChanges();
                    return Ok("OK");
                }
                return BadRequest("No such campaign");
        }       
        #endregion


        #region Content
        [HttpGet("Content")]
        [SwaggerOperation(Description = "Get all content for loggined company with screenshot url links")]
        public async Task<IActionResult> ContentGet([FromHeader] string Authorization)
        {
            if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");             
            var companyId = Guid.Parse(userClaims["companyId"]);
            
            var contents = _context.Contents.Where(x => x.CompanyId == companyId).ToList();
            var result = new List<ContentModel>();
            foreach (var c in contents)
            {
                var screenshotLink = await _sftpClient.GetFileUrl(_containerName + "/" + c.ContentId.ToString() + ".png");
                result.Add(new ContentModel(c, screenshotLink));
            }
            return Ok(result);
        }

        [HttpPost("Content")]
        [SwaggerOperation(Description = "Create new content and save screenshot on sftp server")]
        public async Task<IActionResult> ContentPost([FromBody] ContentModel model, [FromHeader] string Authorization)
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
        [SwaggerOperation(Description = "Edit existing content, remove screenshot from sftp and save new screenshot(if you pass it in json body)")]
        public async Task<IActionResult> ContentPut([FromBody] ContentModel model, [FromHeader] string Authorization)
        {
            if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");         
            Content content = model.Content;
            Content contentEntity = _context.Contents.Where(p => p.ContentId == content.ContentId).FirstOrDefault();
            foreach (var p in typeof(Content).GetProperties())
            {
                if (p.GetValue(content, null) != null)
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
        [SwaggerOperation(Description = "DElete content and remove screenshot from sftp")]
        public async Task<IActionResult> ContentDelete([FromQuery] Guid contentId, [FromHeader] string Authorization)
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
        #region Helper classes
        public class CampaignModel
        {
            public CampaignModel(Campaign cmp, List<CampaignContent> campaignContents)
            {
                Campaign = cmp;
                CampaignContents = campaignContents;
            }
            public Campaign Campaign { get; set; }
            public List<CampaignContent> CampaignContents { get; set; }
        }
        public class ContentModel
        {
            public ContentModel(Content cnt, string screen)
            {
                Content = cnt;
                Screenshot = screen;
            }
            public Content Content { get; set; }
            public string Screenshot { get; set; }
        }
        #endregion
    }
}