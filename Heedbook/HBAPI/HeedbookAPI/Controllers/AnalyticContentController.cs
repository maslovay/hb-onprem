using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Services;
using UserOperations.Models.AnalyticModels;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using UserOperations.Utils;
using UserOperations.Providers;
using System.Threading.Tasks;
using UserOperations.Providers.Interfaces;
using System.IO;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticContentController : Controller
    {
        private readonly IAnalyticContentProvider _analyticContentProvider;
        private readonly IAnalyticCommonProvider _analyticCommonProvider;
        private readonly IHelpProvider _helpProvider;
        private readonly ILoginService _loginService;
        private readonly IRequestFilters _requestFilters;

        public AnalyticContentController(
            IAnalyticContentProvider analyticContentProvider,
            IAnalyticCommonProvider analyticCommonProvider,
            IHelpProvider helpProvider,
            ILoginService loginService,
            IRequestFilters requestFilters
            )
        {
            _analyticContentProvider = analyticContentProvider;
            _analyticCommonProvider = analyticCommonProvider;
            _helpProvider = helpProvider;
            _loginService = loginService;
            _requestFilters = requestFilters;
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

                var dialogue = await _analyticCommonProvider.GetDialogueIncludedFramesByIdAsync(dialogueId);
                if (dialogue == null) return BadRequest("No such dialogue");

                var slideShowSessionsAll = await _analyticContentProvider.GetSlideShowsForOneDialogueAsync(dialogue);

                var contentsShownGroup = slideShowSessionsAll.Where(p => !p.IsPoll)
                    .GroupBy(p => new { p.ContentType,  p.ContentId, p.Url }, (key, group) => new
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
                var answersAmount = slideShowSessionsAll.Where(p => p.IsPoll).Count();
                var answers = await _analyticContentProvider
                    .GetAnswersInOneDialogueAsync(slideShowSessionsAll, dialogue.BegTime, dialogue.EndTime, dialogue.ApplicationUserId);
                  
                var answersByContent = pollAmount.Where(x => x.Key2 != null).Select(x => new
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
                jsonToReturn["AnswersAmount"] = answersAmount;
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

                var dialogues = await _analyticCommonProvider.GetDialoguesInfoWithFramesAsync(begTime, endTime, companyIds, applicationUserIds, workerTypeIds);
                var slideShowSessionsAll = await _analyticContentProvider.GetSlideShowFilteredByPoolAsync(begTime, endTime, companyIds, applicationUserIds, workerTypeIds, false);

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
                var clients = slideShowSessionsAll.Select(x => x.DialogueId).Distinct().Count();
               
                var contentsShownGroup = slideShowSessionsAll
                    .GroupBy(p => new { p.ContentId, p.Url }, (key, group) => new
                    {
                        Key1 = key.ContentId,
                        Key2 = key.Url,
                        Result = group.ToList()
                    }).ToList();

                var contentInfo = new 
                {
                    Views = views,
                    Clients = clients,
                    ContentFullInfo = contentsShownGroup.Where(x => x.Key2 != null).Select(x => new ContentFullOneInfo
                    {
                        Content = x.Key2.ToString(),
                        AmountViews = x.Result.Where(p =>  p.DialogueId != null && p.DialogueId != default(Guid)).Count(),                            
                        EmotionAttention = _analyticContentProvider.EmotionsDuringAdv(x.Result, dialogues),
                        Age = x.Result.Where(p => p.DialogueId != null).Average(p => p.Age),
                        Male = x.Result.Where(p => p.Gender == "male").Count(),
                        Female = x.Result.Where(p => p.Gender == "female").Count()
                    })
                    .Union(contentsShownGroup.Where(x => x.Key2 == null).Select(x => new ContentFullOneInfo
                    {
                        Content = x.Key1.ToString(),
                        AmountViews = x.Result.Where(p => p.DialogueId != null && p.DialogueId != default(Guid)).Count(),//TODO,
                        ContentName = x.Result.FirstOrDefault().ContentName,  
                        EmotionAttention = _analyticContentProvider.EmotionsDuringAdv(x.Result, dialogues),
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
                                                     [FromQuery(Name = "isFile")] bool isFile = false
                                                     )
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
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);

                var dialogues = await _analyticCommonProvider.GetDialoguesInfoWithFramesAsync(begTime, endTime, companyIds, applicationUserIds, workerTypeIds);
                var slideShowSessionsAll = await _analyticContentProvider.GetSlideShowFilteredByPoolAsync(begTime, endTime, companyIds, applicationUserIds, workerTypeIds, true);
                slideShowSessionsAll = _analyticContentProvider.AddDialogueIdToShow(slideShowSessionsAll, dialogues);
                var answers = await _analyticContentProvider.GetAnswersFullAsync(slideShowSessionsAll, begTime, endTime, companyIds, applicationUserIds, workerTypeIds);

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
                    });

                var contentInfo = new
                {
                    Views = slideShowSessionsAll.Count(),
                    Clients = slideShowSessionsAll.Select(x => x.DialogueId).Distinct().Count(),
                    Answers = answers.Count(),
                    Conversion = conversion,
                    ContentFullInfo = slideShowInfoGroupByContent
                };

                if (isFile)
                {
                    MemoryStream excelDocStream = _helpProvider.CreatePoolAnswersSheet(slideShowInfoGroupByContent.ToList());
                    excelDocStream.Seek(0, SeekOrigin.Begin);
                    return new FileStreamResult(excelDocStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                }
                var jsonToReturn = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(contentInfo));
                return Ok(jsonToReturn);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpGet("PollXlsx")]
        public async Task<IActionResult> PollXlsx([FromQuery(Name = "begTime")] string beg,
                                                     [FromQuery(Name = "endTime")] string end,
                                                  [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                  [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                  [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                  [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                  [FromHeader] string Authorization)
        {
            try
            {

              //  _helpProvider.CreatePoolAnswersSheet();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }
    }
}

 
