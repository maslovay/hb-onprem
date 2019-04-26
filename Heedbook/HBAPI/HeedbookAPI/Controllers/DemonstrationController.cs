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
using UserOperations.Models.AnalyticModels;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using HBData;
using HBLib.Utils;
using UserOperations.Utils;
using HBMLHttpClient;
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
        private readonly DBOperations _dbOperation;
        private readonly SftpClient _sftpClient;
        // private readonly HbMlHttpClient _mlclient;
        private readonly ILoginService _loginService;
        private Dictionary<string, string> userClaims;

        public DemonstrationController(
            RecordsContext context,
            IConfiguration config,
            DBOperations dbOperation,
            SftpClient sftpClient,
            //     HbMlHttpClient mlclient,
            ILoginService loginService
            )
        {
            _context = context;
            _config = config;
            _dbOperation = dbOperation;
            _sftpClient = sftpClient;
            //   _mlclient = mlclient;
            _loginService = loginService;
        }

        // [HttpPost("AnalyzeFrames")]
        // public async Task<IActionResult> AnalyzeFramesAsync([FromQuery(Name = "applicationUserId")] Guid applicationUserId, 
        //                                     [FromBody] string fileString)
        // {
        //     try
        //     {
        //         var companyId = _context.ApplicationUsers.First(p => p.Id == applicationUserId).CompanyId;
        //         var curDate = DateTime.Now;
        //         var imgBytes = Convert.FromBase64String(fileString);
        //         var memoryStream = new MemoryStream(imgBytes);

        //         if (FaceDetection.IsFaceDetected(imgBytes, out var faceLength))
        //         {
        //             // to do: base 64
        //             var faceResult = await _mlclient.GetFaceResult(fileString);
        //             var age = faceResult.FirstOrDefault().Attributes.Age;
        //             var gender = faceResult.FirstOrDefault().Attributes.Gender;
        //             var genderId = (gender == "male") ? 1 : 2;

        //             System.Console.WriteLine($"Result of recognition - {age}, {gender}");

        //             var campaigns = _context.CampaignContents
        //                 .Include(p => p.Campaign)
        //                 .Include(p => p.Content)
        //                 .Where(p => p.Campaign.CompanyId == companyId
        //                     && p.Campaign.BegAge <= age
        //                     && p.Campaign.EndAge > age
        //                     && p.Campaign.BegDate <= curDate
        //                     && p.Campaign.EndDate >= curDate
        //                     && p.Campaign.StatusId == 3
        //                     && p.Campaign.IsSplash == false
        //                     && (p.Campaign.GenderId == 0 | p.Campaign.GenderId == genderId))
        //                 .Select(p => new ContentInfo {
        //                     CampaignContentId = p.CampaignId.ToString(),
        //                     Duration = p.Content.Duration,
        //                     RawHtml = p.Content.RawHTML,
        //                     SequenceNumber = p.SequenceNumber,
        //                 }).ToList();


        //             campaigns = campaigns.OrderBy(p => p.CampaignContentId).ThenBy(p => p.SequenceNumber).ToList();
        //             var iteration = 0;
        //             foreach (var campaign in campaigns)
        //             {
        //                 campaign.SequenceNumber = iteration;
        //                 iteration += 1;
        //             };
        //             var result = new Result
        //             {
        //                 Age = (int?) age,
        //                 Gender = gender,
        //                 Content = campaigns
        //             };
        //             return Ok(JsonConvert.SerializeObject(result));

        //         }
        //         else
        //         {
        //             System.Console.WriteLine("no faces detecrted");
        //             var campaigns = _context.CampaignContents
        //                 .Include(p => p.Campaign)
        //                 .Include(p => p.Content)
        //                 .Where(p => p.Campaign.CompanyId == companyId
        //                     && p.Campaign.BegDate <= curDate
        //                     && p.Campaign.EndDate >= curDate
        //                     && p.Campaign.StatusId == 3
        //                     && p.Campaign.IsSplash == true)
        //                 .Select(p => new ContentInfo
        //                 {
        //                     CampaignContentId = p.CampaignContentId.ToString(),
        //                     Duration = p.Content.Duration,
        //                     RawHtml = p.Content.RawHTML,
        //                     SequenceNumber = p.SequenceNumber,
        //                 }).ToList();

        //             campaigns = campaigns.OrderBy(p => p.CampaignContentId).ThenBy(p => p.SequenceNumber).ToList();
        //             var iteration = 0;
        //             foreach (var campaign in campaigns)
        //             {
        //                 campaign.SequenceNumber = iteration;
        //                 iteration += 1;
        //             };
        //             var result = new Result
        //             {
        //                 Age = null,
        //                 Gender = null,
        //                 Content = campaigns
        //             };
        //             return Ok(JsonConvert.SerializeObject(result));  
        //         }
        //     }
        //     catch (Exception e)
        //     {
        //         return BadRequest(e);
        //     }
        // }

      

        [HttpPost("FlushStats")]
        [SwaggerOperation(Summary = "Save contents display", Description = "Saves data about content display on device (content, user, content type, start and end date) for statistic")]
        [SwaggerResponse(400, "Invalid parametrs or error in DB connection", typeof(string))]
        [SwaggerResponse(200, "all sessions were saved")]
        public IActionResult FlushStats([FromBody, 
            SwaggerParameter("campaignContentId, begTime, applicationUserId", Required = true)] 
            List<SlideShowSession> stats)
        {
            try
            {
                foreach (SlideShowSession stat in stats)
                {
                    var html = _context.CampaignContents.Where(x=>x.CampaignContentId == stat.CampaignContentId).Select(x=>x.Content).FirstOrDefault().RawHTML;
                    stat.ContentType = html.Contains("PollAnswer") ? "poll" : "media";
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

                var campaigns = _context.Campaigns
                .Where(p => p.CampaignContents != null && p.CampaignContents.Count() != 0
                    && p.CompanyId == companyId
                    && p.BegDate <= curDate
                    && p.EndDate >= curDate
                    && p.StatusId == 2)
                .Select(p =>
                    new
                    {
                        campaign = p,
                        contents = p.CampaignContents
                                .Select(c => new ContentWithId() { contentWithId = c.Content, campaignContentId = c.CampaignContentId }).ToList()                        
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

                var htmlList = campaigns.SelectMany(x => x.contents.ToDictionary(v => v.htmlId, v => v.contentWithId.RawHTML));

                string videoStrA = "<div id=\"panelsContentWrapper\" style=\"width: 100%; height: 100%; font-size: 16px;\"><div style=\"width: 100%; height: 100%;\"><div id=\"layoutPanel_0\" style=\"height: 100%; width: 100%; position: relative; background: rgb(0, 0, 0);\"><div class=\"BackgroundVideo \" tabindex=\"0\" style=\"position: absolute; top: 0px; left: 0px; width: 100%; height: 100%; visibility: visible; overflow: hidden;\"><video autoplay muted src=\"";
                string videoStrB = "\" preload=\"auto\" poster=\"\" loop=\"\" playsinline=\"\" width=\"1045\" height=\"588\" style=\"position: absolute; width: 1045.33px; height: 588px; top: 0px; left: -1.16667px;\"></video></div></div></div><script>let backgroundCover=(a,b)=>{let e,f,g,i,j,k=b.clientWidth,l=b.clientHeight;e=a instanceof HTMLVideoElement?a.width/a.height:a instanceof HTMLImageElement?void 0===a.naturalWidth?a.width/a.height:a.naturalWidth/a.naturalHeight:a.clientWidth/a.clientHeight,k/l>e?(f=k,g=k/e,i=-(g-l)*0.5,j=0):(f=l*e,g=l,i=0,j=-(f-k)*0.5),b.style.overflow='hidden',a.style.position='absolute',a.width=f,a.height=g,a.style.width=f+'px',a.style.height=g+'px',a.style.top=i+'px',a.style.left=j+'px'};document.addEventListener('DOMContentLoaded',function(){let a=document.querySelectorAll('video');a.forEach(b=>backgroundCover(b,b.parentElement))});</script></div><script>document.addEventListener(\"DOMContentLoaded\", function() {\n            var wrapperDom = document.getElementById(\"panelsContentWrapper\");\n            changeFontSizeScaleHandler (wrapperDom.offsetWidth, wrapperDom.offsetHeight, 1000, 600, 16, 0.016);\n        });\n        function changeFontSizeScaleHandler (width, height, startWidth, startHeight, startFontSize, stepFontSize) {\n            width = parseInt(width);\n            height = parseInt(height);\n            var additionalWidth = width - startWidth;\n            var additionalHeight = height - startHeight;\n            if (!additionalWidth && !additionalHeight)\n                return false;\n    \n            var changeValue = 0;\n            if ((additionalWidth > 0 && additionalHeight > 0) || (additionalWidth < 0 && additionalHeight < 0)) {\n                changeValue = additionalWidth;\n                if (additionalHeight < additionalWidth)\n                    changeValue = additionalHeight;\n            }\n    \n            if (additionalWidth > 0 && additionalHeight < 0)\n                changeValue = additionalHeight;\n            \n            if (additionalWidth < 0 && additionalHeight > 0)\n                changeValue = additionalWidth;\n    \n            var changeFontSizeValue = stepFontSize * changeValue;\n            var newEditorFontSize = parseInt(startFontSize) + changeFontSizeValue;\n            document.getElementById(\"panelsContentWrapper\").style.fontSize = newEditorFontSize + 'px';\n        };</script>";
                IEnumerable<FileInfoModel> media = null;
                try
                {
                   media = await _sftpClient.GetAllFilesData(containerName, companyId.ToString());
                }
                catch
                {
                   // return BadRequest("This company has no any content");
                }
                List<object> resultMedia = new List<object>();
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
                            var fileInfo = media.Where(x=>x.url == link).FirstOrDefault();
                            resultMedia.Add(fileInfo);
                        }
                        match = match.NextMatch();
                    }
                    if (resultInput.Contains("<video"))
                    {
                        resultInput = videoStrA + link + videoStrB;
                    }
                    htmlList2.Add(contains.Key, resultInput);
                }


                var responseContent = new List<object>();
                responseContent.Add(new { campaigns = campaignsList });
                responseContent.Add(new { htmlRaws = htmlList2 });
                responseContent.Add(new { blobMedia = resultMedia });
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
            catch
            {
                return BadRequest("Error");
            }
        }
    }


   
}