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
        [HttpGet("Campaign")]
        [SwaggerOperation(Description = "Return all camapigns for loggined company with content relations")]
        public IEnumerable<Campaign> CampaignGet()
        {             
            Guid? companyId = GetCompanyIdFromToken();
            if (companyId == null) return null;

            var campaigns = _context.Campaigns.Include(x => x.CampaignContents).Where(x=>x.CompanyId == companyId).ToList();
            return campaigns;
        }

        [HttpPost("Campaign")]
        [SwaggerOperation(Description = "Create new campaign with content relations")]
        public Campaign CampaignPost([FromBody] CampaignModel model)
        {
            Guid? companyId = GetCompanyIdFromToken();
            if (companyId == null) return null;

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
            return campaign;
        }

        [HttpPut("Campaign")]
        [SwaggerOperation(Description = "Edit existing campaign. Remove all content relations and create new")]
        public Campaign CampaignPut([FromBody] CampaignModel model)
        {
             if (!Request.Headers.TryGetValue("Authorization", out StringValues authToken)) return null;
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
                return campaignEntity;
        }

        [HttpDelete("Campaign")]
        [SwaggerOperation(Description = "Set camapign status Inactive and delete all content relations for this campaign")]
        public IActionResult CampaignDelete([FromQuery] Guid campaignId)
        {
          if (!Request.Headers.TryGetValue("Authorization", out StringValues authToken)) return BadRequest("Token error");
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
        public async Task<IEnumerable<ContentModel>> ContentGet()
        {
            Guid? companyId = GetCompanyIdFromToken();
            if (companyId == null) return null; 
            
            var contents = _context.Contents.Where(x => x.CompanyId == companyId).ToList();
            var result = new List<ContentModel>();
            foreach (var c in contents)
            {
                var screenshotLink = await _sftpClient.GetFileUrl(_containerName + "/" + c.ContentId.ToString() + ".png");
                result.Add(new ContentModel(c, screenshotLink));
            }
            return result;
        }

        [HttpPost("Content")]
        [SwaggerOperation(Description = "Create new content and save screenshot on sftp server")]
        public async Task<ContentModel> ContentPost([FromBody] ContentModel model)
        {
            Guid? companyId = GetCompanyIdFromToken();
            if (companyId == null) return null; 
            
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
            return model;
        }

        [HttpPut("Content")]
        [SwaggerOperation(Description = "Edit existing content, remove screenshot from sftp and save new screenshot(if you pass it in json body)")]
        public async Task<ContentModel> ContentPut([FromBody] ContentModel model)
        {
            if (!Request.Headers.TryGetValue("Authorization", out StringValues authToken)) return null;
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
            return model;
        }

        [HttpDelete("Content")]
        [SwaggerOperation(Description = "DElete content and remove screenshot from sftp")]
        public async Task<IActionResult> ContentDelete([FromQuery] Guid contentId)
        {
            if (!Request.Headers.TryGetValue("Authorization", out StringValues authToken))  return BadRequest("Token error");
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
        private Guid? GetCompanyIdFromToken()
        {
            try
            {
            if (!Request.Headers.TryGetValue("Authorization", out StringValues authToken)) return null;
                string token = authToken.First();
                var claims = _loginService.GetDataFromToken(token);
                return Guid.Parse(claims["companyId"]);
            }
            catch
            {
                return null;
            }
        }

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
    }
}