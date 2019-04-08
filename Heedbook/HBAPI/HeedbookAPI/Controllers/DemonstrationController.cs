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

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DemonstrationController : Controller
    {
        private readonly RecordsContext _context;
        private readonly IConfiguration _config;
        private readonly DBOperations _dbOperation;

        private readonly HbMlHttpClient _mlclient;

        public DemonstrationController(
            RecordsContext context,
            IConfiguration config,
            DBOperations dbOperation,
            HbMlHttpClient mlclient
            )
        {
            _context = context;
            _config = config;
            _dbOperation = dbOperation;
        }

        [HttpPost("AnalyzeFrames")]
        public async Task<IActionResult> AnalyzeFramesAsync([FromQuery(Name = "applicationUserId")] Guid applicationUserId, 
                                            [FromBody] string fileString)
        {
            try
            {
                var companyId = _context.ApplicationUsers.First(p => p.Id == applicationUserId).CompanyId;
                var curDate = DateTime.Now;
                var imgBytes = Convert.FromBase64String(fileString);
                var memoryStream = new MemoryStream(imgBytes);

                if (FaceDetection.IsFaceDetected(imgBytes, out var faceLength))
                {
                    // to do: base 64
                    var faceResult = await _mlclient.GetFaceResult(fileString);
                    var age = faceResult.FirstOrDefault().Attributes.Age;
                    var gender = faceResult.FirstOrDefault().Attributes.Gender;
                    var genderId = (gender == "male") ? 1 : 2;

                    System.Console.WriteLine($"Result of recognition - {age}, {gender}");
                    
                    var campaigns = _context.CampaignContents
                        .Include(p => p.Campaign)
                        .Include(p => p.Content)
                        .Where(p => p.Campaign.CompanyId == companyId
                            && p.Campaign.BegAge <= age
                            && p.Campaign.EndAge > age
                            && p.Campaign.BegDate <= curDate
                            && p.Campaign.EndDate >= curDate
                            && p.Campaign.StatusId == 3
                            && p.Campaign.IsSplash == false
                            && (p.Campaign.GenderId == 0 | p.Campaign.GenderId == genderId))
                        .Select(p => new ContentInfo {
                            CampaignContentId = p.CampaignId.ToString(),
                            Duration = p.Content.Duration,
                            RawHtml = p.Content.RawHTML,
                            SequenceNumber = p.SequenceNumber,
                        }).ToList();


                    campaigns = campaigns.OrderBy(p => p.CampaignContentId).ThenBy(p => p.SequenceNumber).ToList();
                    var iteration = 0;
                    foreach (var campaign in campaigns)
                    {
                        campaign.SequenceNumber = iteration;
                        iteration += 1;
                    };
                    var result = new Result
                    {
                        Age = (int?) age,
                        Gender = gender,
                        Content = campaigns
                    };
                    return Ok(JsonConvert.SerializeObject(result));

                }
                else
                {
                    System.Console.WriteLine("no faces detecrted");
                    var campaigns = _context.CampaignContents
                        .Include(p => p.Campaign)
                        .Include(p => p.Content)
                        .Where(p => p.Campaign.CompanyId == companyId
                            && p.Campaign.BegDate <= curDate
                            && p.Campaign.EndDate >= curDate
                            && p.Campaign.StatusId == 3
                            && p.Campaign.IsSplash == true)
                        .Select(p => new ContentInfo
                        {
                            CampaignContentId = p.CampaignContentId.ToString(),
                            Duration = p.Content.Duration,
                            RawHtml = p.Content.RawHTML,
                            SequenceNumber = p.SequenceNumber,
                        }).ToList();

                    campaigns = campaigns.OrderBy(p => p.CampaignContentId).ThenBy(p => p.SequenceNumber).ToList();
                    var iteration = 0;
                    foreach (var campaign in campaigns)
                    {
                        campaign.SequenceNumber = iteration;
                        iteration += 1;
                    };
                    var result = new Result
                    {
                        Age = null,
                        Gender = null,
                        Content = campaigns
                    };
                    return Ok(JsonConvert.SerializeObject(result));  
                }
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }


        [HttpPost("ContentSession")]
        public IActionResult ContentSession(
            [FromBody] ContentInfoStructure content)
        {
            try
            {
                var session  = new CampaignContentSession {
                    CampaignContentSessionId = Guid.NewGuid(),
                    ApplicationUserId = content.ApplicationUserId,
                    BegTime = DateTime.UtcNow,
                    CampaignContentId = content.CampaignContentId

                };
                _context.CampaignContentSessions.Add(session);
                _context.SaveChanges();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }

        }

        [HttpPost("FlushStats")]
        public IActionResult FlushStats(
            [FromBody] List<ContentInfoStructure> stats)
        {
            try
            {
                foreach (ContentInfoStructure stat in stats)
                {
                    var campaignContentId = stat.CampaignContentId;
                    var applicationUserId = stat.ApplicationUserId;

                    var session = new CampaignContentSession{
                        CampaignContentSessionId = Guid.NewGuid(),
                        ApplicationUserId = applicationUserId,
                        BegTime = DateTime.UtcNow,
                        CampaignContentId = campaignContentId
                    };

                    _context.CampaignContentSessions.Add(session);
                    _context.SaveChanges();

                }
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }


        // [HttpPost("GetContents")]
        // public IActionResult GetContents(
        //     [FromQuery] Guid applicationUserId,
        //     [FromQuery] string containerName, 
        //     [FromQuery] string directoryName,
        //     [FromQuery] string fileName,
        //     [FromQuery] string beg
        //     )
        // {
        //     try
        //     {
        //         var companyId = _context.ApplicationUsers.First(p => p.Id == applicationUserId).CompanyId;
        //         var curDate = DateTime.Now;
        //         containerName = String.IsNullOrEmpty(containerName) ? "mediacontent" : containerName; 

        //         var htmlList = new Hashtable();
        //         var campaigns = _context.CampaignContents
        //             .Include(p => p.Campaign)
        //             .Where(p => p.Campaign.CompanyId == companyId
        //                 && p.Campaign.BegDate <= curDate
        //                 && p.Campaign.EndDate >= curDate
        //                 && p.Campaign.StatusId == 3)
        //             .GroupBy(p => p.Campaign)
        //             .ToList();
                
        //         var campaignsList = campaigns.Select(p => new Campaign
        //         {
        //             Id = p.First().CampaignId,
        //             Gender = p.First().Campaign.GenderId,
        //             BegAge = (int)p.First().Campaign.BegAge,
        //             EndAge = (int)p.First().Campaign.EndAge,
        //             IsSplashScreen = p.First().Campaign.IsSplash,
        //             Content = p.Select(q =>
        //                new { info = _context.Contents.Where(c => c.ContentId.ToString() == q.ContentId.ToString()).First(),
        //                      id = q.CampaignContentId
        //                }).Select(content => {
        //                     string htmlId = Guid.NewGuid().ToString();
        //                     htmlList.Add(htmlId, content.info.RawHTML.ToString());
        //                     return new Content
        //                     {
        //                         Id = content.Id,
        //                         HTML = htmlId,
        //                         Duration = content.info.Duration,
        //                         Type = content.info.RawHTML.Contains("PollAnswer") ? "poll" : "media"
        //                     };
        //                 }).ToList()
        //         }).ToList();

        //         var directories = new Guid?[] { companyId };
        //         var htmlList2 = new Hashtable();
        //         var begTime = !String.IsNullOrEmpty(beg) ? DateTime.ParseExact(beg, "yyyyMMdd", CultureInfo.InvariantCulture) : DateTime.Now.AddDays(-6);
                
                
        //         string videoStrA = "<div id=\"panelsContentWrapper\" style=\"width: 100%; height: 100%; font-size: 16px;\"><div style=\"width: 100%; height: 100%;\"><div id=\"layoutPanel_0\" style=\"height: 100%; width: 100%; position: relative; background: rgb(0, 0, 0);\"><div class=\"BackgroundVideo \" tabindex=\"0\" style=\"position: absolute; top: 0px; left: 0px; width: 100%; height: 100%; visibility: visible; overflow: hidden;\"><video autoplay muted src=\"";
        //         string videoStrB = "\" preload=\"auto\" poster=\"\" loop=\"\" playsinline=\"\" width=\"1045\" height=\"588\" style=\"position: absolute; width: 1045.33px; height: 588px; top: 0px; left: -1.16667px;\"></video></div></div></div><script>let backgroundCover=(a,b)=>{let e,f,g,i,j,k=b.clientWidth,l=b.clientHeight;e=a instanceof HTMLVideoElement?a.width/a.height:a instanceof HTMLImageElement?void 0===a.naturalWidth?a.width/a.height:a.naturalWidth/a.naturalHeight:a.clientWidth/a.clientHeight,k/l>e?(f=k,g=k/e,i=-(g-l)*0.5,j=0):(f=l*e,g=l,i=0,j=-(f-k)*0.5),b.style.overflow='hidden',a.style.position='absolute',a.width=f,a.height=g,a.style.width=f+'px',a.style.height=g+'px',a.style.top=i+'px',a.style.left=j+'px'};document.addEventListener('DOMContentLoaded',function(){let a=document.querySelectorAll('video');a.forEach(b=>backgroundCover(b,b.parentElement))});</script></div><script>document.addEventListener(\"DOMContentLoaded\", function() {\n            var wrapperDom = document.getElementById(\"panelsContentWrapper\");\n            changeFontSizeScaleHandler (wrapperDom.offsetWidth, wrapperDom.offsetHeight, 1000, 600, 16, 0.016);\n        });\n        function changeFontSizeScaleHandler (width, height, startWidth, startHeight, startFontSize, stepFontSize) {\n            width = parseInt(width);\n            height = parseInt(height);\n            var additionalWidth = width - startWidth;\n            var additionalHeight = height - startHeight;\n            if (!additionalWidth && !additionalHeight)\n                return false;\n    \n            var changeValue = 0;\n            if ((additionalWidth > 0 && additionalHeight > 0) || (additionalWidth < 0 && additionalHeight < 0)) {\n                changeValue = additionalWidth;\n                if (additionalHeight < additionalWidth)\n                    changeValue = additionalHeight;\n            }\n    \n            if (additionalWidth > 0 && additionalHeight < 0)\n                changeValue = additionalHeight;\n            \n            if (additionalWidth < 0 && additionalHeight > 0)\n                changeValue = additionalWidth;\n    \n            var changeFontSizeValue = stepFontSize * changeValue;\n            var newEditorFontSize = parseInt(startFontSize) + changeFontSizeValue;\n            document.getElementById(\"panelsContentWrapper\").style.fontSize = newEditorFontSize + 'px';\n        };</script>";
                
        //         var media = HeedbookMessengerStatic.BlobStorageMessenger.GetBlobData(containerName, directoryName ?? directories, false, fileName, null, begTime);
        //         List<object> resultMedia = new List<object>();
                
        //         //string oldSite = "hbpromoblobstorage.blob.core.windows.net";
        //         //string newSite = "wantadblobstorage.blob.core.windows.net";
        //         string unmutedVideo = "<video ";
        //         string mutedVideo = "<video autoplay muted ";

        //         foreach (DictionaryEntry contains in htmlList)
        //         {
        //             string input = contains.Value.ToString();
        //             //string resultInput = Regex.Replace(input, oldSite, newSite);
        //             string resultInput = Regex.Replace(input, unmutedVideo, mutedVideo);
        //             resultInput = resultInput.Replace("&amp", "");
        //             string pattern = @"(http|ftp|https)://([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,;@?^=%&:/~+#-]*[\w@?;^=%&/~+#-])?";
        //             Regex regex = new Regex(pattern);
        //             Match match = regex.Match(resultInput);
        //             while (match.Success)
        //             {
        //                 string link = match.Value;
        //                 link = link.Replace("&amp", "");
        //                 foreach (var mediaFile in media)
        //                 {
        //                     if (link.Contains(mediaFile.blobName))
        //                     {
        //                         if (resultInput.Contains("<video"))
        //                         {
        //                             resultInput = videoStrA + mediaFile.blobName + videoStrB;
        //                             resultMedia.Add(mediaFile);
        //                             break;
        //                         }
        //                         else
        //                         {
        //                             var mediaName = link.Contains("&quot;") ? mediaFile.blobName + "&quot;" : mediaFile.blobName;
        //                             var newResultInput = resultInput.Replace(link, mediaName);
        //                             if (newResultInput != resultInput)
        //                             {
        //                                 resultInput = newResultInput;
        //                                 resultMedia.Add(mediaFile);
        //                                 break;
        //                             }
        //                         }
        //                     }
        //                 }
        //                 match = match.NextMatch();
        //             }
        //             htmlList2.Add(contains.Key, resultInput);
        //         }


        //         var responseContent = new List<object>();
        //         responseContent.Add(new { campaigns = campaignsList });
        //         responseContent.Add(new { htmlRaws = htmlList2 });
        //         responseContent.Add(new { blobMedia = resultMedia });

        //         return Ok();
        //     }
        //     catch (Exception e)
        //     {
        //         return BadRequest(e);
        //     }
        // }
    }


    public class Result
    {
        public string Gender;
        public int? Age;
        public List<ContentInfo> Content;
    }

    public class ContentInfo
    {
        public string CampaignContentId;
        public int Duration;
        public string RawHtml;
        public int SequenceNumber;
    }

    public class ContentInfoStructure
    {
        public Guid CampaignContentId;
        public Guid ApplicationUserId;
    }

     public class Content
    {
        public string Id;
        public string HTML;
        public int Duration;
        public string Type;
    }

    public class Campaign
    {
        public Guid Id;
        public int Gender;
        public int BegAge;
        public int EndAge;
        public List<Content> Content;
        public bool IsSplashScreen;
    }
}