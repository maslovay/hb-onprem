using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using HBData.Models;
using Microsoft.EntityFrameworkCore;
using HBLib.Utils;
using System.Collections;
using System.Text.RegularExpressions;
using static HBLib.Utils.SftpClient;
using UserOperations.CommonModels;
using HBData.Repository;

namespace UserOperations.Services
{
    public class DemonstrationV2Service
    {
        private readonly SftpClient _sftpClient;
        private readonly IGenericRepository _repository;
        private readonly LoginService _loginService;

        public DemonstrationV2Service(
            SftpClient sftpClient,
            IGenericRepository repository,
            LoginService loginService
            )
        {
            _sftpClient = sftpClient;
            _repository = repository;
            _loginService = loginService;
        }

        public async Task FlushStats( List<SlideShowSession> stats)
        {
            var userId = _loginService.GetCurrentUserId();
            foreach (SlideShowSession stat in stats)
            {
                if(stat.ContentType == "url")
                    stat.IsPoll = false;
                else
                {
                    var html = _repository.GetAsQueryable<CampaignContent>()
                        .Where(x => x.CampaignContentId == stat.CampaignContentId)
                        .Select(x => x.Content.RawHTML).FirstOrDefault();

                    stat.IsPoll = html.Contains("PollAnswer") ? true : false;
                }
                stat.SlideShowSessionId = Guid.NewGuid();
                stat.ApplicationUserId = userId;
                await _repository.CreateAsync<SlideShowSession>(stat);
                await _repository.SaveAsync();
            }
        }
        public async Task<List<object>> GetContents()
        {
            var companyId = _loginService.GetCurrentUserId();
            var curDate = DateTime.Now;
            var containerName = "media";
            var active = _repository.GetAsQueryable<Status>().Where(p => p.StatusName == "Active").FirstOrDefault().StatusId;

            var campaigns = _repository.GetAsQueryable<Campaign>()
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
                .Union(_repository.GetAsQueryable<Content>().Where( c => c.CompanyId == companyId && c.StatusId == active && (c.CampaignContents == null || c.CampaignContents.Count() == 0))
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
            return responseContent;
        }
        public async Task<string> PollAnswer( CampaignContentAnswer answer)
        {
            answer.CampaignContentAnswerId = Guid.NewGuid();
            answer.ApplicationUserId = _loginService.GetCurrentUserId();
            //answer.Time = DateTime.UtcNow;
            await _repository.CreateAsync<CampaignContentAnswer>(answer);
            await _repository.SaveAsync();
            return "Saved";
        }
    }   
}