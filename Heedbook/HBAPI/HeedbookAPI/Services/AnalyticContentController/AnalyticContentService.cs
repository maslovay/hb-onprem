using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using UserOperations.Utils;
using UserOperations.Providers;
using System.Threading.Tasks;
using UserOperations.Providers.Interfaces;
using System.IO;
using UserOperations.Models.Get.AnalyticContentController;
using HBData.Repository;
using UserOperations.Utils.AnalyticContentUtils;

namespace UserOperations.Services
{
    public class AnalyticContentService : Controller
    {
        private readonly IAnalyticContentProvider _analyticContentProvider;
        private readonly IHelpProvider _helpProvider;
        private readonly ILoginService _loginService;
        private readonly IRequestFilters _requestFilters;
        private readonly IGenericRepository _repository;
        private readonly AnalyticContentUtils _utils;

        public AnalyticContentService(
            IAnalyticContentProvider analyticContentProvider,
            IHelpProvider helpProvider,
            ILoginService loginService,
            IRequestFilters requestFilters,
            IGenericRepository repository,
            AnalyticContentUtils utils
            )
        {
            _analyticContentProvider = analyticContentProvider;
            _helpProvider = helpProvider;
            _loginService = loginService;
            _requestFilters = requestFilters;
            _repository = repository;
            _utils = utils;
        }

//---FOR ONE DIALOGUE---
        [HttpGet("ContentShows")]
        public async Task<IActionResult> ContentShows([FromQuery(Name = "dialogueId")] Guid dialogueId,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                // _log.Info("ContentShows/ContentShows started");
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");

                var dialogue = await _analyticContentProvider.GetDialogueIncludedFramesByIdAsync(dialogueId);
                if (dialogue == null) return BadRequest("No such dialogue");

                var slideShowSessionsAll = await _analyticContentProvider.GetSlideShowsForOneDialogueAsync(dialogue);

                var contentsShownGroup = slideShowSessionsAll.Where(p => !p.IsPoll)
                    .GroupBy(p => new { p.ContentType,  p.ContentId, p.Url }, (key, group) => new
                    {
                        Key1 = key.ContentType,
                        Key2 = key.ContentId,
                        Key3 = key.Url,
                        Result = group.ToList()
                    });
                var contentInfo = new //ContentTotalInfo
                {
                    ContentsAmount = slideShowSessionsAll.Where(p => !p.IsPoll).Count(),
                    ContentsInfo = contentsShownGroup.Where(x => x.Key2 != null).Select(x => new ContentOneInfo
                    {
                        Content = x.Key2.ToString(),
                        AmountOne = x.Result.Count(),
                        ContentType = x.Key1,
                        ContentName = x.Result.FirstOrDefault().ContentName,
                        EmotionAttention = _analyticContentProvider.EmotionDuringAdvOneDialogue(x.Result, dialogue.DialogueFrame.ToList())
                    })
                    .Union(contentsShownGroup.Where(x => x.Key2 == null).Select(x => new ContentOneInfo
                    {
                        Content = x.Key3,
                        AmountOne = x.Result.Count(),
                        ContentType = x.Key1,
                        EmotionAttention = _analyticContentProvider.EmotionDuringAdvOneDialogue(x.Result, dialogue.DialogueFrame.ToList()),
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
                var answers = await _analyticContentProvider
                    .GetAnswersInOneDialogueAsync(slideShowSessionsAll, dialogue.BegTime, dialogue.EndTime, dialogue.ApplicationUserId);
                  
                var answersByContent = pollAmount.Where(x => x.Key2 != null)
                    .Select(x => new
                        {
                            Content = x.Key2.ToString(),
                            AmountShowsOneContent = x.Result.Count(),
                            ContentType = x.Key1,
                            Answers = answers
                                .Where(p => x.Result
                                    .Select(r => r.CampaignContentId).Contains(p.CampaignContentId))
                                .Select(p => new { p.Answer, p.Time }),
                            EmotionAttention = _analyticContentProvider.EmotionDuringAdvOneDialogue(x.Result, dialogue.DialogueFrame.ToList()),
                        })
                    .ToList();

                var jsonToReturn = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(contentInfo));

                jsonToReturn["AnswersInfo"] = answersByContent;
                jsonToReturn["AnswersAmount"] = slideShowSessionsAll.Where(p => p.IsPoll).Count();
                return Ok(jsonToReturn);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpGet("Efficiency")]
        public async Task<IActionResult> Efficiency([FromQuery(Name = "begTime")] string beg,
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
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);

                var dialogues = await _analyticContentProvider.GetDialoguesInfoWithFramesAsync(begTime, endTime, companyIds, applicationUserIds, workerTypeIds);
                //var slideShowSessionsAll = await _analyticContentProvider.GetSlideShowFilteredByPoolAsync(begTime, endTime, companyIds, applicationUserIds, workerTypeIds, false);
               
                //foreach ( var session in slideShowSessionsAll )
                //{
                //    var dialog = dialogues.FirstOrDefault(x => x.BegTime <= session.BegTime && x.EndTime >= session.BegTime && x.ApplicationUserId == session.ApplicationUserId);
                //    session.DialogueId = dialog?.DialogueId;
                //    session.DialogueFrames = dialog?.DialogueFrame;
                //    session.Age = dialog?.Age;
                //    session.Gender = dialog?.Gender;
                //}
                var slideShowSessionsInDialogues = await _analyticContentProvider
                    .GetSlideShowWithDialogueIdFilteredByPoolAsync(begTime, endTime, companyIds, applicationUserIds, workerTypeIds, false, dialogues);
                var views = slideShowSessionsInDialogues.Count();
                var clients = slideShowSessionsInDialogues.Select(x => x.DialogueId).Distinct().Count();
               
                var contentsShownGroup = slideShowSessionsInDialogues
                    .GroupBy(p => new { p.ContentId, p.Url }, (key, group) => new
                    {
                        Key1 = key.ContentId,
                        Key2 = key.Url,
                        Result = group.ToList()
                    }).ToList();
               // var splashShows = slideShowSessionsAll.Where(x => x.Campaign != null && x.Campaign.IsSplash).Count();
                var splashViews = slideShowSessionsInDialogues.Where(x => x.Campaign != null && x.Campaign.IsSplash).Count();

                var contentInfo = new 
                {
                    Views = views - splashViews,
                    Clients = clients,
                    SplashViews = splashViews,
                    ContentFullInfo = contentsShownGroup.Where(x => x.Key2 != null).Select(x => new ContentFullOneInfo
                    {
                        Content = x.Key2.ToString(),
                        AmountViews = x.Result.Where(p =>  p.DialogueId != null && p.DialogueId != default(Guid)).Count(),                            
                        EmotionAttention = _analyticContentProvider.EmotionsDuringAdv(x.Result),
                        Age = x.Result.Where(p => p.DialogueId != null).Average(p => p.Age),
                        Male = x.Result.Where(p => p.Gender == "male").Count(),
                        Female = x.Result.Where(p => p.Gender == "female").Count()
                    })
                    .Union(contentsShownGroup.Where(x => x.Key2 == null).Select(x => new ContentFullOneInfo
                    {
                        Content = x.Key1.ToString(),
                        AmountViews = x.Result.Where(p => p.DialogueId != null && p.DialogueId != default(Guid)).Count(),//TODO,
                        ContentName = x.Result.FirstOrDefault().ContentName,  
                        EmotionAttention = _analyticContentProvider.EmotionsDuringAdv(x.Result),
                        Age = x.Result.Where(p => p.DialogueId != null).Average(p => p.Age),
                        Male = x.Result.Where(p => p.Gender == "male").Count(),
                        Female = x.Result.Where(p => p.Gender == "female").Count()
                    }
                    )).ToList()
                };              
                var jsonToReturn = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(contentInfo));
                return Ok(jsonToReturn);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpGet("Poll")]
        public async Task<IActionResult> Poll([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end,
                                                     [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                     [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                     [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                     [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                     [FromHeader] string Authorization,
                                                     [FromQuery(Name = "type")] string type = "json"
                                                     )
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);

                var dialogues = await _analyticContentProvider.GetDialogueInfos(begTime, endTime, companyIds, applicationUserIds, workerTypeIds);
                var slideShowSessionsAll = await _analyticContentProvider
                    .GetSlideShowWithDialogueIdFilteredByPoolAsync(begTime, endTime, companyIds, applicationUserIds, workerTypeIds, true, dialogues);
                var answers = await _analyticContentProvider
                    .GetAnswersFullAsync(slideShowSessionsAll, begTime, endTime, companyIds, applicationUserIds, workerTypeIds);

                double conversion = _analyticContentProvider.GetConversion(slideShowSessionsAll.Count(), answers.Count());

                var slideShowInfoGroupByContent = slideShowSessionsAll
                    .GroupBy(p => p.ContentId)
                    .Select(ssh => new AnswerInfo
                    {
                        Content = ssh.Key.ToString(),
                        AmountViews = ssh.Count(),
                        ContentName = ssh.FirstOrDefault().ContentName,
                        Answers = _analyticContentProvider.GetAnswersForOneContent(answers, ssh.Key),
                        AnswersAmount = _analyticContentProvider.GetAnswersForOneContent(answers, ssh.Key).Count(),
                        Conversion = (double)_analyticContentProvider.GetAnswersForOneContent(answers, ssh.Key).Count() / (double)ssh.Count()
                    }).ToList();

                var contentInfo = new
                {
                    Views = slideShowSessionsAll.Count(),
                    Clients = slideShowSessionsAll.Select(x => x.DialogueId).Distinct().Count(),
                    Answers = slideShowInfoGroupByContent.Sum(x => x.AnswersAmount), //answers.Count(),//                    
                    Conversion = conversion,
                    ContentFullInfo = slideShowInfoGroupByContent
                };

                if (type != "json")
                {
                    MemoryStream excelDocStream = _utils.CreatePoolAnswersSheet(slideShowInfoGroupByContent.ToList(), $"{begTime.ToShortDateString()}_{endTime.ToShortDateString()}");
                    excelDocStream.Seek(0, SeekOrigin.Begin);
                    return File(excelDocStream, "application/octet-stream", "answers.xls");
                }
                var jsonToReturn = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(contentInfo));
                return Ok(jsonToReturn);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }
    }
}

 
