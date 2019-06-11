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
using UserOperations.Utils;
using HBLib.Utils;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticContentController : Controller
    {
        //  private readonly IConfiguration _config;
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
        //  private readonly DBOperations _dbOperation;
        // private readonly RequestFilters _requestFilters;
        private readonly ElasticClient _log;


        public AnalyticContentController(
            // IConfiguration config,
            ILoginService loginService,
            RecordsContext context,
            //  DBOperations dbOperation,
            // RequestFilters requestFilters,
            ElasticClient log
            )
        {
            //_config = config;
            _loginService = loginService;
            _context = context;
            //  _dbOperation = dbOperation;
            //  _requestFilters = requestFilters;
            _log = log;
        }

        [HttpGet("ContentShows")]
        public IActionResult ContentShows([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "dialogueId")] Guid dialogueId,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                _log.Info("ContentShows/ContentShows started");
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");

                var dialogue = _context.Dialogues.Where(p => p.StatusId == 3 && p.DialogueId == dialogueId).FirstOrDefault();
                Console.WriteLine("----------1----------------");
                //-----------------------------------BY CONTENT --------------------------------
                var contentsShownAmount = _context.SlideShowSessions
                    .Include(p => p.CampaignContent)
                    .Where(p => p.BegTime >= dialogue.BegTime
                            && p.BegTime <= dialogue.EndTime
                            && p.ApplicationUserId == dialogue.ApplicationUserId)
                    .GroupBy(p => new { p.ContentType, p.CampaignContent.ContentId, p.Url }, (key, group) => new
                    {
                        Key1 = key.ContentType,
                        Key2 = key.ContentId,
                        key3 = key.Url,
                        Result = group.ToList()
                    });
                var amountShows = _context.SlideShowSessions.Where(p => p.BegTime >= dialogue.BegTime && p.BegTime <= dialogue.EndTime).Count();
                Console.WriteLine("----------3----------------");
                var contentInfo = new ContentTotalInfo
                {
                    ContentsShowsAmount = amountShows,
                    ContentOneInfos = contentsShownAmount.Where(x => x.Key2 != null).Select(x => new ContentOneInfo
                    {
                        Content = x.Key2.ToString(),
                        AmountShowsOneContent = x.Result.Count(),
                        ContentType = x.Key1
                    })
                    .Union(contentsShownAmount.Where(x => x.Key2 == null).Select(x => new ContentOneInfo
                    {
                        Content = x.Result.Select(c => c.Url).FirstOrDefault(),
                        AmountShowsOneContent = x.Result.Count(),
                        ContentType = x.Key1
                    }
                    ))
                    .ToList()
                };
                Console.WriteLine("----------4----------------");
                //-----------------------------------BY TIME ON LINE--------------------------------
                var contentsShowsTime = _context.SlideShowSessions
                                  .Include(p => p.CampaignContent)
                                  .Where(p => p.BegTime >= dialogue.BegTime && p.BegTime <= dialogue.EndTime)
                                  .Select(p => new
                                  {
                                      p.CampaignContentId,
                                      p.BegTime,
                                      p.EndTime,
                                      ContentId = p.CampaignContent != null ? p.CampaignContent.ContentId : null,
                                      p.ApplicationUserId,
                                      p.Url,
                                      p.ContentType
                                  })
                                        .OrderBy(p => p.BegTime).ToList();
                Console.WriteLine("----------5----------------");
                var jsonToReturn = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(contentInfo));

                jsonToReturn["contentsShowsTime"] = contentsShowsTime;
                jsonToReturn["dialogue"] = dialogue;
                _log.Info("ContentShows/ContentShows finished");
                return Ok(jsonToReturn);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }

        }
    }


    public class ContentTotalInfo
    {
        // the total number of demonstrated content, images, videos, URLs 
        // within the campaigns and the employees themselves during the dialogue
        public int ContentsShowsAmount { get; set; }
        public List<ContentOneInfo> ContentOneInfos { get; set; }
    }
    public class ContentOneInfo
    {
        public string Content { get; set; }
        public string ContentType { get; set; }
        public int AmountShowsOneContent { get; set; }
    }
}
