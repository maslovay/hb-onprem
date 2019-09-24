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
        private readonly DBOperations _dbOperation;
        private readonly RequestFilters _requestFilters;
        // private readonly ElasticClient _log;


        public AnalyticContentController(
            // IConfiguration config,
            ILoginService loginService,
            RecordsContext context,
            DBOperations dbOperation,
            RequestFilters requestFilters
            // ElasticClient log
            )
        {
            //_config = config;
            _loginService = loginService;
            _context = context;
            _dbOperation = dbOperation;
            _requestFilters = requestFilters;
            // _log = log;
        }

//---FOR ONE DIALOGUE---
        [HttpGet("ContentShows")]
        public IActionResult ContentShows([FromQuery(Name = "dialogueId")] Guid dialogueId,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                // _log.Info("ContentShows/ContentShows started");
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");

                var dialogue = _context.Dialogues
                    .Include(p => p.DialogueFrame)
                    .Where(p => p.DialogueId == dialogueId).FirstOrDefault();
                if (dialogue == null) return BadRequest("No such dialogue");

                var slideShowSessionsAll = _context.SlideShowSessions
                    .Include(p => p.CampaignContent)
                    .ThenInclude(p => p.Content)
                    .Where(p => p.BegTime >= dialogue.BegTime
                             && p.BegTime <= dialogue.EndTime
                             && p.ApplicationUserId == dialogue.ApplicationUserId)
                             .Select(p =>
                                 new SlideShowInfo
                                 {
                                     BegTime = p.BegTime,
                                     ContentId = p.CampaignContent != null ? p.CampaignContent.ContentId : null,
                                     CampaignContentId = p.CampaignContentId,
                                     ContentType = p.ContentType,
                                     ContentName = p.CampaignContent.Content.Name,
                                     EndTime = p.EndTime,
                                     IsPoll = p.IsPoll,
                                     Url = p.Url,
                                     ApplicationUserId = (Guid)p.ApplicationUserId,
                                     EmotionAttention = _dbOperation.SatisfactionDuringAdv(p, dialogue)
                                 }
                             )
                            .ToList();

                var contentsShownGroup = slideShowSessionsAll.Where(p => !p.IsPoll)
                    .GroupBy(p => new { p.ContentType, ContentId = p.ContentId, p.Url }, (key, group) => new
                    {
                        Key1 = key.ContentType,
                        Key2 = key.ContentId,
                        Key3 = key.Url,
                        Result = group.ToList()
                    }).ToList();

                var amountShows = slideShowSessionsAll.Where(p => !p.IsPoll).Count();
                var contentInfo = new //ContentTotalInfo
                {
                    ContentsAmount = amountShows,
                    ContentsInfo = contentsShownGroup.Where(x => x.Key2 != null).Select(x => new ContentOneInfo
                    {
                        Content = x.Key2.ToString(),
                        AmountOne = x.Result.Count(),
                        ContentType = x.Key1,
                        ContentName = x.Result.FirstOrDefault().ContentName,
                        EmotionAttention = _dbOperation.SatisfactionDuringAdv(x.Result, dialogue)
                    })
                    .Union(contentsShownGroup.Where(x => x.Key2 == null).Select(x => new ContentOneInfo
                    {
                        Content = x.Key3,
                        AmountOne = x.Result.Count(),
                        ContentType = x.Key1,
                        EmotionAttention = _dbOperation.SatisfactionDuringAdv(x.Result, dialogue),
                    }
                    )).ToList()
                };

                var pollAmount = slideShowSessionsAll.Where(p => p.IsPoll && p.ContentId != null)
                    .GroupBy(p => new { p.ContentType, p.ContentId }, (key, group) => new
                    {
                        Key1 = key.ContentType,
                        Key2 = key.ContentId,
                        Result = group.ToList()
                    });
                var answersAmount = slideShowSessionsAll.Where(p => p.IsPoll).Count();
                var answers = _context.CampaignContentAnswers
                        .Where(p => slideShowSessionsAll
                        .Select(x => x.CampaignContentId)
                        .Distinct()
                        .Contains(p.CampaignContentId ) && p.Time >= dialogue.BegTime && p.Time <= dialogue.EndTime).ToList();
                  
                var answersByContent = pollAmount.Where(x => x.Key2 != null).Select(x => new
                {
                    Content = x.Key2.ToString(),
                    AmountShowsOneContent = x.Result.Count(),
                    ContentType = x.Key1,
                    Answers = answers
                            .Where(p => x.Result
                            .Select(r => r.CampaignContentId).Contains(p.CampaignContentId))
                            .Select(p => new { p.Answer, p.Time }),
                    EmotionAttention = _dbOperation.SatisfactionDuringAdv(x.Result, dialogue),
                })
                    .ToList();

                var jsonToReturn = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(contentInfo));

                jsonToReturn["ContentByTime"] = slideShowSessionsAll.OrderBy(p => p.BegTime);
                jsonToReturn["AnswersInfo"] = answersByContent;
                jsonToReturn["AnswersAmount"] = answersAmount;
                // _log.Info("ContentShows/ContentShows finished");
                return Ok(jsonToReturn);
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred {e}");
                return BadRequest(e);
            }
        }

        [HttpGet("Efficiency")]
        public IActionResult Efficiency([FromQuery(Name = "begTime")] string beg,
                                                           [FromQuery(Name = "endTime")] string end,
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRoles(ref companyIds, corporationIds, role, companyId);

                var dialogues = _context.Dialogues
                    .Include(p => p.ApplicationUser)
                    .Include(p => p.DialogueClientSatisfaction)
                    .Include(p => p.DialogueFrame)
                    .Where(p => p.BegTime >= begTime
                            && p.EndTime <= endTime
                            && p.StatusId == 3
                            && p.InStatistic == true
                            && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                            && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                            && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)))
                    .Select(p => new DialogueInfoWithFrames
                    {
                        DialogueId = p.DialogueId,
                        ApplicationUserId = p.ApplicationUserId,
                        BegTime = p.BegTime,
                        EndTime = p.EndTime,
                        DialogueFrame = p.DialogueFrame.ToList(),
                        Gender = p.DialogueClientProfile.Max(x => x.Gender),
                        Age = p.DialogueClientProfile.Average(x => x.Age)

                    })
                    .ToList();

                var slideShowSessionsAll = _context.SlideShowSessions.Where(p => !p.IsPoll)
                    .Include(p => p.ApplicationUser)
                    .Include(p => p.CampaignContent)
                    .ThenInclude(p => p.Content)
                    .Include(p => p.CampaignContent)
                    .ThenInclude(p => p.Campaign)
                    .Where(p => p.BegTime >= begTime
                             && p.BegTime <= endTime
                             && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                             && (!applicationUserIds.Any() || applicationUserIds.Contains((Guid)p.ApplicationUserId))
                             && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)))
                             .Select(p =>
                                 new SlideShowInfo
                                 {
                                     BegTime = p.BegTime,
                                     ContentId = p.CampaignContent != null ? p.CampaignContent.ContentId : null,
                                     CampaignContent = p.CampaignContent,
                                     ContentType = p.ContentType,
                                     ContentName = p.CampaignContent != null ? p.CampaignContent.Content.Name : null,
                                     EndTime = p.EndTime,
                                     IsPoll = p.IsPoll,
                                     Url = p.Url,
                                     ApplicationUserId = (Guid)p.ApplicationUserId
                                 }
                             )
                            .ToList();

                foreach ( var session in slideShowSessionsAll )
                {
                    var dialog =  dialogues.Where(x => x.BegTime <= session.BegTime &&  x.EndTime >= session.BegTime)
                            .FirstOrDefault();
                    session.DialogueId = dialog?.DialogueId;
                    session.Age = dialog?.Age;
                    session.Gender = dialog?.Gender;
                }
                slideShowSessionsAll = slideShowSessionsAll.Where(x => x.DialogueId != null && x.DialogueId != default(Guid)).ToList();

                var views = slideShowSessionsAll.Count();
               // var splashShows = slideShowSessionsAll.Where(x => x.CampaignContent!= null &&  x.CampaignContent.Campaign!= null && x.CampaignContent.Campaign.IsSplash).Count();
                var clients = slideShowSessionsAll.Select(x => x.DialogueId).Distinct().Count();
               
                var contentsShownGroup = slideShowSessionsAll
                    .GroupBy(p => new { ContentId = p.ContentId, p.Url }, (key, group) => new
                    {
                        Key1 = key.ContentId,
                        Key2 = key.Url,
                        Result = group.ToList()
                    }).ToList();
                var contentInfo = new //ContentTotalInfoEfficiency
                {
                    Views = views,
                    Clients = clients,
                    ContentFullInfo = contentsShownGroup.Where(x => x.Key2 != null).Select(x => new ContentFullOneInfo
                    {
                        Content = x.Key2.ToString(),
                        AmountViews = x.Result.Where(p =>  p.DialogueId != null && p.DialogueId != default(Guid)).Count(),                            
                        EmotionAttention = _dbOperation.SatisfactionDuringAdv(x.Result, dialogues),
                        Age = x.Result.Where(p => p.DialogueId != null).Average(p => p.Age),
                        Male = x.Result.Where(p => p.Gender == "male").Count(),
                        Female = x.Result.Where(p => p.Gender == "female").Count()
                    })
                    .Union(contentsShownGroup.Where(x => x.Key2 == null).Select(x => new ContentFullOneInfo
                    {
                        Content = x.Key1.ToString(),
                        AmountViews = x.Result.Where(p => p.DialogueId != null && p.DialogueId != default(Guid)).Count(),//TODO,
                        ContentName = x.Result.FirstOrDefault().ContentName,  
                        EmotionAttention = _dbOperation.SatisfactionDuringAdv(x.Result, dialogues),
                        Age = x.Result.Where(p => p.DialogueId != null).Average(p => p.Age),
                        Male = x.Result.Where(p => p.Gender == "male").Count(),
                        Female = x.Result.Where(p => p.Gender == "female").Count()
                    }
                    )).ToList()
                };              
                var jsonToReturn = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(contentInfo));

                // _log.Info("ContentShows/ContentShows finished");
                return Ok(jsonToReturn);
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred {e}");
                return BadRequest(e);
            }

        }


          [HttpGet("Poll")]
        public IActionResult Poll([FromQuery(Name = "begTime")] string beg,
                                                           [FromQuery(Name = "endTime")] string end,
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                // _log.Info("AnalyticContent/Poll started");
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRoles(ref companyIds, corporationIds, role, companyId);

                var dialogues = _context.Dialogues
                    .Include(p => p.ApplicationUser)
                    .Include(p => p.DialogueClientSatisfaction)
                    .Include(p => p.DialogueFrame)
                    .Where(p => p.BegTime >= begTime
                            && p.EndTime <= endTime
                            && p.StatusId == 3
                            && p.InStatistic == true
                            && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                            && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                            && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)))
                    .Select(p => new DialogueInfoWithFrames
                    {
                        DialogueId = p.DialogueId,
                        ApplicationUserId = p.ApplicationUserId,
                        BegTime = p.BegTime,
                        EndTime = p.EndTime,
                        DialogueFrame = p.DialogueFrame.ToList(),
                        Gender = p.DialogueClientProfile.Max(x => x.Gender),
                        Age = p.DialogueClientProfile.Average(x => x.Age)

                    })
                    .ToList();

                var slideShowSessionsAll = _context.SlideShowSessions.Where(p => p.IsPoll)
                    .Include(p => p.ApplicationUser)
                    .Include(p => p.CampaignContent)
                    .ThenInclude(p => p.Content)
                    .Include(p => p.CampaignContent)
                    .ThenInclude(p => p.Campaign)
                    .Where(p => p.BegTime >= begTime
                             && p.BegTime <= endTime
                             && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                             && (!applicationUserIds.Any() || applicationUserIds.Contains((Guid)p.ApplicationUserId))
                             && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId))
                             && p.CampaignContent != null)
                             .Select(p =>
                                 new SlideShowInfo
                                 {
                                     BegTime = p.BegTime,
                                     ContentId = p.CampaignContent.ContentId,
                                     CampaignContent = p.CampaignContent,
                                     ContentType = p.ContentType,
                                     ContentName = p.CampaignContent.Content.Name,
                                     EndTime = p.EndTime,
                                     ApplicationUserId = (Guid)p.ApplicationUserId
                                 }
                             )
                            .ToList();

                foreach ( var session in slideShowSessionsAll )
                {
                    var dialog =  dialogues.Where(x => x.BegTime <= session.BegTime &&  x.EndTime >= session.BegTime)
                            .FirstOrDefault();
                    session.DialogueId = dialog?.DialogueId;
                }
                slideShowSessionsAll =  slideShowSessionsAll.Where(x => x.DialogueId != null && x.DialogueId != default(Guid)).ToList();
                var answersList = _context.CampaignContentAnswers
                        .Where(p => slideShowSessionsAll
                        .Select(x => x.CampaignContent.CampaignContentId)
                        .Distinct()
                        .Contains(p.CampaignContentId) && p.Time >= begTime && p.Time <= endTime).ToList();

                var views = slideShowSessionsAll.Count();
                var clients =slideShowSessionsAll.Select(x => x.DialogueId).Distinct().Count();
                var answers = answersList.Count();
                double conversion = views != 0 ? (double)answers / (double)views : 0;
               
                var contentsShownGroup = slideShowSessionsAll
                    .GroupBy(p => p.ContentId).ToList();
                    
                var contentInfo = new
                {
                    Views = views,
                    Clients = clients,
                    Answers = answers,
                    Conversion = conversion,
                    ContentFullInfo = contentsShownGroup.Select(x => new AnswerInfo
                    {
                        Content = x.Key.ToString(),
                        AmountViews = x.Count(),// x.Where(p => p.DialogueId != null && p.DialogueId != default(Guid)).Count(),//TODO,
                        ContentName = x.FirstOrDefault().ContentName,
                        Answers = answersList
                            .Where(p => x.Select(r => r.CampaignContent.CampaignContentId).Contains(p.CampaignContentId))
                            .Select(p => new AnswerInfo.AnswerOne { Answer =  p.Answer, Time = p.Time }).ToList(),
                        AnswersAmount = answersList
                            .Where(p => x.Select(r => r.CampaignContent.CampaignContentId).Contains(p.CampaignContentId))
                            .Select(p => new { p.Answer, p.Time }).Count(),
                        Conversion = (double)answersList
                            .Where(p => x.Select(r => r.CampaignContent.CampaignContentId).Contains(p.CampaignContentId)).Count() / (double)x.Count()
                    }
                    ).ToList()};
                var jsonToReturn = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(contentInfo));

                // _log.Info("ContentShows/Poll finished");
                return Ok(jsonToReturn);
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred {e}");
                return BadRequest(e);
            }

        }
    }


    // public class ContentTotalInfo
    // {
    //     // the total number of demonstrated content, images, videos, URLs 
    //     // within the campaigns and the employees themselves during the dialogue
    //     public int ContentsAmount { get; set; }
    //     public List<ContentOneInfo> ContentsInfo { get; set; }
    // }

    // public class ContentTotalInfoEfficiency
    // {
    //     // the total number of demonstrated content, images, videos, URLs 
    //     // within the campaigns and the employees themselves during the dialogue
    //     public int Views { get; set; }
    //     public int Clients { get; set; }
    //      public List<ContentFullOneInfo> ContentFullInfo { get; set; }
    // }
    public class ContentOneInfo
    {
        public string Content { get; set; }
        public string ContentType { get; set; }
        public string ContentName { get; set; }
        public int AmountOne { get; set; }
        public EmotionAttention EmotionAttention { get; set; }
    }

    public class ContentFullOneInfo
    {
        public string Content { get; set; }
        public string ContentName { get; set; }
        public int AmountViews { get; set; }
        public EmotionAttention EmotionAttention { get; set; }
        public int Male { get; set; }
        public int Female { get; set; }
        public double? Age { get; set; }
    }
    public class AnswerInfo
    {
        public string Content { get; set; }
        public int AnswersAmount { get; set; }
        public string ContentName { get; set; }
        public int AmountViews { get; set; }
        public double Conversion { get; set; }
        public List<AnswerOne> Answers {get; set;}
        public class AnswerOne
        {
            public string Answer { get; set; }
            public DateTime Time { get; set; }
        }

    }
}

 
