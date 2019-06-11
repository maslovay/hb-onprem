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

                var dialogue = _context.Dialogues.Where(p => p.DialogueId == dialogueId).FirstOrDefault();
                if (dialogue == null) return BadRequest("No such dialogue");

//-------------------------------ALL CONTENT SESSIONS --------------------------------
                var slideShowSessionsAll = _context.SlideShowSessions
                    .Include(p => p.CampaignContent)
                    .Where(p => p.BegTime >= dialogue.BegTime
                             && p.BegTime <= dialogue.EndTime
                             && p.ApplicationUserId == dialogue.ApplicationUserId)
                             .Select(p =>
                                 new
                                 {
                                     p.BegTime,
                                     ContentId = p.CampaignContent != null ? p.CampaignContent.ContentId : null,
                                     p.CampaignContentId,
                                     p.ContentType,
                                     p.EndTime,
                                     p.IsPoll,
                                     p.Url,
                                     p.ApplicationUserId
                                 }
                             )
                            .ToList();

 //-------------------------------GROUPING BY CONTENT (NOT POLL)--------------------------------
                var contentsShownGroup = slideShowSessionsAll.Where(p => !p.IsPoll)
                    .GroupBy(p => new { p.ContentType, ContentId = p.ContentId, p.Url }, (key, group) => new
                    {
                        Key1 = key.ContentType,
                        Key2 = key.ContentId,
                        Key3 = key.Url,
                        Result = group
                    }).ToList();

                var amountShows = slideShowSessionsAll.Where(p => !p.IsPoll).Count();
                var contentInfo = new ContentTotalInfo
                {
                    ContentsAmount = amountShows,
                    ContentsInfo = contentsShownGroup.Where(x => x.Key2 != null).Select(x => new ContentOneInfo
                    {
                        Content = x.Key2.ToString(),
                        AmountOne = x.Result.Count(),
                        ContentType = x.Key1
                    })
                    .Union(contentsShownGroup.Where(x => x.Key2 == null).Select(x => new ContentOneInfo
                    {
                        Content = x.Key3,
                        AmountOne = x.Result.Count(),
                        ContentType = x.Key1
                    }
                    )).ToList()
                };

//------------------------------------ GROUPING BY CONTENT POOLS-----------------------------------------
                    var pollAmount = slideShowSessionsAll.Where(p => p.IsPoll && p.ContentId != null)
                        .GroupBy(p => new { p.ContentType, p.ContentId }, (key, group) => new
                        {
                            Key1 = key.ContentType,
                            Key2 = key.ContentId,
                            Result = group.ToList()
                        });
                    var answersAmount = slideShowSessionsAll.Where(p => p.IsPoll).Count();
                    //---all answers during dialogue--- 
                    var answers = _context.CampaignContentAnswers
                            .Where(p => p.Time > dialogue.BegTime
                            && p.Time < dialogue.EndTime
                            && p.ApplicationUserId == dialogue.ApplicationUserId).ToList();
                    //---answers grouping by content---                  
                    var answersByContent = pollAmount.Where(x => x.Key2 != null).Select(x => new
                        {
                            Content = x.Key2.ToString(),
                            AmountShowsOneContent = x.Result.Count(),
                            ContentType = x.Key1,
                            Answers = answers
                                .Where(p => x.Result
                                .Select(r => r.CampaignContentId).Contains(p.CampaignContentId))
                                .Select(p => new { p.Answer, p.Time })
                        })
                        .ToList();

                    Console.WriteLine("----------5----------------");
                    var jsonToReturn = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(contentInfo));

                    jsonToReturn["ContentByTime"] = slideShowSessionsAll.OrderBy(p => p.BegTime);
                    jsonToReturn["AnswersInfo"] = answersByContent;
                    jsonToReturn["AnswersAmount"] = answersAmount;
                    jsonToReturn["Dialogue"] = dialogue;
                    _log.Info("ContentShows/ContentShows finished");
                    return Ok(jsonToReturn);
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occurred {e}");
                return BadRequest(e);
            }

        }
    }


    public class ContentTotalInfo
    {
        // the total number of demonstrated content, images, videos, URLs 
        // within the campaigns and the employees themselves during the dialogue
        public int ContentsAmount { get; set; }
        public List<ContentOneInfo> ContentsInfo { get; set; }
    }
    public class ContentOneInfo
    {
        public string Content { get; set; }
        public string ContentType { get; set; }
        public int AmountOne { get; set; }
    }
}
