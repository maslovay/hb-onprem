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

using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using HBData;
using HBLib.Utils;
using HBLib;


namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CampaignContentController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _config;
        private readonly ITokenService _tokenService;
        private readonly RecordsContext _context;
        private readonly SftpClient _sftpClient;



        public CampaignContentController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration config,
            ITokenService tokenService,
            RecordsContext context,            
            SftpClient sftpClient
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
            _tokenService = tokenService;
            _context = context;
            _sftpClient = sftpClient;
        }
        #region Campaign
        [HttpGet("Campaign")]
        public IEnumerable<Campaign> CampaignGet()
        {
            var campaigns = _context.Campaigns.Include(x=>x.CampaignContents).ToList();
            return campaigns;
        }

        [HttpPost("Campaign")]
        public Campaign CampaignPost([FromBody] CampaignModel model)
        {
            Campaign campaign = model.Campaign;
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
        public Campaign CampaignPut([FromBody] CampaignModel model)
        {
            Campaign modelCampaign = model.Campaign;
            var campaign = _context.Campaigns.Include(x=>x.CampaignContents).Where(p => p.CampaignId == modelCampaign.CampaignId).FirstOrDefault();
            if (campaign == null)
            {
                return null;
            }
            else
            {
                foreach (var campCont in campaign.CampaignContents)
                {
                   _context.Remove(campCont);
                }
                foreach (var p in typeof(Campaign).GetProperties())
                {
                    if (p.GetValue(modelCampaign, null) != null)
                        p.SetValue(campaign, p.GetValue(modelCampaign, null), null);
                }
                _context.SaveChanges();
                foreach (var campCont in model.CampaignContents)
                {
                   campCont.CampaignId = campaign.CampaignId;
                   _context.Add(campCont);
                }
                _context.SaveChanges();
            }
            return campaign;
        }
        [HttpDelete("Campaign")]
        public IActionResult CampaignDelete([FromQuery] Guid campaignId)
        {             
            var campaign = _context.Campaigns.Include(x=>x.CampaignContents).Where(p => p.CampaignId == campaignId).FirstOrDefault();
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
        public IEnumerable<Content> ContentGet()
        {
            var contents = _context.Contents.ToList();
            return contents;
        }
        [HttpPost("Content")]
        public async Task<Content> ContentPost([FromBody] ContentModel model)
        {
            Content content = model.Content;
            content.CreationDate = DateTime.UtcNow;
            content.UpdateDate = DateTime.UtcNow;
         //   content.StatusId = _context.Statuss.Where(p => p.StatusName == "Active").FirstOrDefault().StatusId;;
            _context.Add(content);
            _context.SaveChanges();   
            string base64 = model.Screenshot;
            Byte[] imgBytes = Convert.FromBase64String(base64);
            Stream blobStream = new MemoryStream(imgBytes);
            await _sftpClient.UploadAsMemoryStreamAsync(blobStream, "test", content.ContentId.ToString()+ ".png");
            return content;
        }
        #endregion

    public struct CampaignModel
    {
        public CampaignModel(Campaign cmp, List<CampaignContent> campaignContents)
        {
            Campaign = cmp;
            CampaignContents = campaignContents;
        }
        public Campaign Campaign { get; set; }
        public List<CampaignContent> CampaignContents { get; set; }
    }
    public struct ContentModel
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