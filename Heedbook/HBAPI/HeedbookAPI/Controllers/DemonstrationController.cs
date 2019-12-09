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
using HBLib.Utils;
using UserOperations.Utils;
using System.Collections;
using System.Text.RegularExpressions;
using static HBLib.Utils.SftpClient;
using Swashbuckle.AspNetCore.Annotations;
using UserOperations.CommonModels;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DemonstrationController : Controller
    {
        private readonly RecordsContext _context;
        private readonly IConfiguration _config;
        private readonly IDBOperations _dbOperation;
        private readonly SftpClient _sftpClient;
        private readonly ILoginService _loginService;

        public DemonstrationController(
            RecordsContext context,
            IConfiguration config,
            IDBOperations dbOperation,
            SftpClient sftpClient,
            ILoginService loginService
            )
        {
            _context = context;
            _config = config;
            _dbOperation = dbOperation;
            _sftpClient = sftpClient;
            _loginService = loginService;
        }      

        [HttpPost("FlushStats")]
        [SwaggerOperation(Summary = "Save contents display", Description = "Saves data about content display on device (content, user, content type, start and end date) for statistic")]
        [SwaggerResponse(400, "Invalid parametrs or error in DB connection", typeof(string))]
        [SwaggerResponse(200, "all sessions were saved")]
        public IActionResult FlushStats([FromBody, 
            SwaggerParameter("campaignContentId, applicationUserId, begTime, endTime, contentType", Required = true)] 
            List<SlideShowSession> stats)
        {
            try
            {          
                foreach (SlideShowSession stat in stats)
                {
                    if(stat.ContentType == "url")
                    {
                        stat.IsPoll = false;
                    }
                    else
                    {
                        var html = _context.CampaignContents.Where(x=>x.CampaignContentId == stat.CampaignContentId).Select(x=>x.Content).FirstOrDefault().RawHTML;
                        stat.IsPoll = html.Contains("PollAnswer") ? true : false;
                    }
                    stat.SlideShowSessionId = Guid.NewGuid();
                    _context.Add(stat);
                    _context.SaveChanges();
                }    
                return Ok("Saved");
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }


        [HttpGet("GetContents")]
        [SwaggerOperation(Summary = "Return content on device", Description = "Get all content for loggined company with RowHtml data and url on media. Specially  for device")]
        [SwaggerResponse(400, "Invalid userId or error in DB connection", typeof(string))]
        [SwaggerResponse(200, "Content", typeof(ContentReturnOnDeviceModel))]
        public async Task<ActionResult> GetContents([FromQuery] string userId)
        {
            try
            {                        
                var companyId = _context.ApplicationUsers.Where(x => x.Id.ToString() == userId).FirstOrDefault().CompanyId;
                var curDate = DateTime.Now;
                var containerName = "media";
                var active = _context.Statuss.Where(p => p.StatusName == "Active").FirstOrDefault().StatusId;

                var campaigns = _context.Campaigns
                .Where(p => p.CampaignContents != null && p.CampaignContents.Count() != 0
                    && p.CompanyId == companyId
                    && p.BegDate <= curDate
                    && p.EndDate >= curDate
                    && p.StatusId == active)
                .Select(p =>
                    new
                    {
                        campaign = p,
                        contents = p.CampaignContents.Where(x=> x.StatusId == active)
                                .Select(c => new ContentWithId() { contentWithId = c.Content, campaignContentId = c.CampaignContentId, htmlId = c.ContentId.ToString() }).ToList()                        
                    }                
                ).ToList();

                var campaignsList = campaigns.Select(p => new CampaignModel
                {
                    Id = p.campaign.CampaignId,
                    Gender = p.campaign.GenderId,
                    BegAge = p.campaign.BegAge,
                    EndAge = p.campaign.EndAge,
                    IsSplashScreen = p.campaign.IsSplash,
                    Content = p.contents.Select(q => new ContentModel
                    {
                        Id = q.contentWithId.ContentId,
                        CampaignContentId = q.campaignContentId,
                        HTML = q.htmlId,
                        Duration = q.contentWithId.Duration,
                        Type = q.contentWithId.RawHTML.Contains("PollAnswer") ? "poll" : "media"
                    }).ToList()
                }).ToList();

                var htmlList = campaigns
                    .SelectMany(x => x.contents
                    .ToDictionary(v => v.htmlId, v => v.contentWithId.RawHTML))
                    .Union(_context.Contents.Where( c => c.CompanyId == companyId && c.StatusId == active && (c.CampaignContents == null || c.CampaignContents.Count() == 0))
                    .Select(c => new ContentWithId() { contentWithId = c, htmlId = c.ContentId.ToString() }).ToList()
                    .ToDictionary(v => v.htmlId, v => v.contentWithId.RawHTML).AsEnumerable());

                string videoStrA = "<div id=\"panelsContentWrapper\" style=\"width: 100%; height: 100%; font-size: 16px;\"><div style=\"width: 100%; height: 100%;\"><div id=\"layoutPanel_0\" style=\"height: 100%; width: 100%; position: relative; background: rgb(0, 0, 0);\"><div class=\"BackgroundVideo \" tabindex=\"0\" style=\"position: absolute; top: 0px; left: 0px; width: 100%; height: 100%; visibility: visible; overflow: hidden;\"><video autoplay muted src=\"";
                string videoStrB = "\" preload=\"auto\" poster=\"\" loop=\"\" playsinline=\"\" width=\"1045\" height=\"588\" style=\"position: absolute; width: 1045.33px; height: 588px; top: 0px; left: -1.16667px;\"></video></div></div></div><script>let backgroundCover=(a,b)=>{let e,f,g,i,j,k=b.clientWidth,l=b.clientHeight;e=a instanceof HTMLVideoElement?a.width/a.height:a instanceof HTMLImageElement?void 0===a.naturalWidth?a.width/a.height:a.naturalWidth/a.naturalHeight:a.clientWidth/a.clientHeight,k/l>e?(f=k,g=k/e,i=-(g-l)*0.5,j=0):(f=l*e,g=l,i=0,j=-(f-k)*0.5),b.style.overflow='hidden',a.style.position='absolute',a.width=f,a.height=g,a.style.width=f+'px',a.style.height=g+'px',a.style.top=i+'px',a.style.left=j+'px'};document.addEventListener('DOMContentLoaded',function(){let a=document.querySelectorAll('video');a.forEach(b=>backgroundCover(b,b.parentElement))});</script></div><script>document.addEventListener(\"DOMContentLoaded\", function() {\n            var wrapperDom = document.getElementById(\"panelsContentWrapper\");\n            changeFontSizeScaleHandler (wrapperDom.offsetWidth, wrapperDom.offsetHeight, 1000, 600, 16, 0.016);\n        });\n        function changeFontSizeScaleHandler (width, height, startWidth, startHeight, startFontSize, stepFontSize) {\n            width = parseInt(width);\n            height = parseInt(height);\n            var additionalWidth = width - startWidth;\n            var additionalHeight = height - startHeight;\n            if (!additionalWidth && !additionalHeight)\n                return false;\n    \n            var changeValue = 0;\n            if ((additionalWidth > 0 && additionalHeight > 0) || (additionalWidth < 0 && additionalHeight < 0)) {\n                changeValue = additionalWidth;\n                if (additionalHeight < additionalWidth)\n                    changeValue = additionalHeight;\n            }\n    \n            if (additionalWidth > 0 && additionalHeight < 0)\n                changeValue = additionalHeight;\n            \n            if (additionalWidth < 0 && additionalHeight > 0)\n                changeValue = additionalWidth;\n    \n            var changeFontSizeValue = stepFontSize * changeValue;\n            var newEditorFontSize = parseInt(startFontSize) + changeFontSizeValue;\n            document.getElementById(\"panelsContentWrapper\").style.fontSize = newEditorFontSize + 'px';\n        };</script>";
                IEnumerable<FileInfoModel> media = null;
                try
                {
                   media = await _sftpClient.GetAllFilesData(containerName, companyId.ToString());
                }
                catch
                {
                }
                string unmutedVideo = "<video ";
                string mutedVideo = "<video autoplay muted ";

                var htmlList2 = new Hashtable();
                foreach (KeyValuePair<string, string> contains in htmlList)
                {
                    string input = contains.Value.ToString();
                    string resultInput = Regex.Replace(input, unmutedVideo, mutedVideo);
                    resultInput = resultInput.Replace("&amp", "");
                    string pattern = @"(http|ftp|https)://([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,;@?^=%&:/~+#-]*[\w@?;^=%&/~+#-])?";
                    Regex regex = new Regex(pattern);
                    Match match = regex.Match(resultInput);
                    string link = null;
                    while (match.Success)
                    {
                        link = match.Value;
                        if(link.Contains(containerName))
                        {
                            link = link.Replace("&amp", "");
                            link = link.Replace("&quot;", "");
                        }
                        match = match.NextMatch();
                    }
                    if (resultInput.Contains("<video"))
                    {
                        resultInput = videoStrA + link + videoStrB;
                    }
                    htmlList2.Add(contains.Key, resultInput);
                }


                var responseContent = new List<object>
                {
                    new { campaigns = campaignsList },
                    new { htmlRaws = htmlList2 },
                    new { blobMedia = media }
                };
                return Ok(responseContent);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }
    
        [HttpPost("PollAnswer")]
        [SwaggerOperation(Summary = "Save answer from poll", Description = "Receive answer from device ande save it connected to campaign and content")]
        [SwaggerResponse(400, "Invalid data or error in DB connection", typeof(string))]
        [SwaggerResponse(200, "Saved")]
        public async Task<IActionResult> PollAnswer([FromBody] CampaignContentAnswer answer)
        {
            try
            {    
                answer.CampaignContentAnswerId = Guid.NewGuid();
                answer.Time = DateTime.UtcNow;
                await _context.AddAsync(answer);
                await _context.SaveChangesAsync();
                return Ok("Saved");
            }
            catch( Exception e)
            {
                return BadRequest("Error "+ e.Message );
            }
        }
    }   
}