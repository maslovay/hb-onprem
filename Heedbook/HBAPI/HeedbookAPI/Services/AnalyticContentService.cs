using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using UserOperations.Utils;
using System.Threading.Tasks;
using System.IO;
using HBData.Repository;
using UserOperations.Utils.AnalyticContentUtils;
using UserOperations.Controllers;
using HBData.Models;
using UserOperations.Models.AnalyticModels;
using UserOperations.Utils.CommonOperations;

namespace UserOperations.Services
{
    public class AnalyticContentService
    {
        private readonly SpreadsheetDocumentUtils _helpProvider;
        private readonly LoginService _loginService;
        private readonly RequestFilters _requestFilters;
        private readonly IGenericRepository _repository;
        private readonly AnalyticContentUtils _utils;
        private readonly FileRefUtils _fileRef;

        private const string _containerName = "screenshots";

        public AnalyticContentService(
            SpreadsheetDocumentUtils helpProvider,
            LoginService loginService,
            RequestFilters requestFilters,
            IGenericRepository repository,
            AnalyticContentUtils utils,
            FileRefUtils fileRef
            )
        {
            _helpProvider = helpProvider;
            _loginService = loginService;
            _requestFilters = requestFilters;
            _repository = repository;
            _utils = utils;
            _fileRef = fileRef;
        }

        //---FOR ONE DIALOGUE---
        public async Task<Dictionary<string, object>> ContentShows(Guid dialogueId)
        {
            var dialogue = await GetDialogueIncludedFramesByIdAsync(dialogueId);
            if (dialogue == null) throw new NoFoundException("No such dialogue");

            var slideShowSessionsAll = GetSlideShowsForOneDialogueAsync(dialogue);

            var contentShown = slideShowSessionsAll.Where(p => !p.IsPoll).ToList();
            var pollShown = slideShowSessionsAll.Where(p => p.IsPoll && p.ContentId != null).ToList();

            var contentsShownGroup = contentShown
                .GroupBy(p => new { p.ContentType, p.ContentId, p.Url }, (key, group) => new
                {
                    Key1 = key.ContentType,
                    Key2 = key.ContentId,
                    Key3 = key.Url,
                    Result = group.ToList()
                }).ToList();


            var contentInfo = new //ContentTotalInfo
            {
                ContentsAmount = contentShown.Count(),
                ContentsInfo = contentsShownGroup.Where(x => x.Key2 != null).Select(x => new ContentFullOneInfo
                {
                    Content = x.Key2.ToString(),
                    AmountViews = x.Result.Count(),
                    ContentType = x.Key1,
                    ContentName = x.Result.FirstOrDefault().ContentName,
                    FtpLink = _fileRef.GetFileLink(_containerName, x.Key2 + ".png", default) + $"?{x.Result.FirstOrDefault().ContentUpdateDate}",
                    EmotionAttention = EmotionDuringAdvOneDialogue(x.Result, dialogue.DialogueFrame.ToList())
                })
                .Union(contentsShownGroup.Where(x => x.Key2 == null) != null ? contentsShownGroup.Where(x => x.Key2 == null).Select(x => new ContentFullOneInfo
                {
                    Content = null,
                    AmountViews = x.Result.Count(),
                    ContentType = x.Key1,
                    EmotionAttention = EmotionDuringAdvOneDialogue(x.Result, dialogue.DialogueFrame.ToList()),
                    ExternalLink = x.Key3.ToString(),
                }
                ) : null)
                .ToList()
            };
            List<CampaignContentAnswer> answers = await GetAnswersInOneDialogueAsync(dialogue.BegTime, dialogue.EndTime, dialogue.DeviceId);

            var pollShownGroup = pollShown
                .GroupBy(p => new { p.ContentType, p.ContentId }, (key, group) => new
                {
                    Key1 = key.ContentType,
                    Key2 = key.ContentId,
                    Result = group.ToList(),
                    FtpLink = _fileRef.GetFileLink(_containerName, key.ContentId + ".png", default) + $"?{group.FirstOrDefault().ContentUpdateDate}",
                });


            var answersByContent = pollShownGroup.Where(x => x.Key2 != null)
                .Select(x => new
                {
                    Content = x.Key2.ToString(),
                    AmountShowsOneContent = x.Result.Count(),
                    ContentType = "poll",
                    Answers = answers
                            .Where(p => x.Result
                                .Select(r => r.CampaignContentId).Contains(p.CampaignContentId))
                            .Select(p => new { p.Answer, p.Time }),
                    EmotionAttention = EmotionDuringAdvOneDialogue(x.Result, dialogue.DialogueFrame.ToList()),
                    x.FtpLink
                })
                .ToList();

            //var answersByContent = answers.GroupBy(x => x.CampaignContent.ContentId)
            //   .Where(x => pollShown.Where(p => p.ContentId == x.Key).Count() != 0)
            //   .Select(x => new
            //   {
            //       Content = x.Key.ToString(),
            //       AmountShowsOneContent = pollShown.Where(p => p.ContentId == x.Key).Count(),
            //       ContentType = "poll",
            //       Answers = x.Select(p => new { p.Answer, p.Time }),
            //       EmotionAttention = EmotionDuringAdvOneDialogue(pollShown.Where(p => p.ContentId == x.Key).ToList(), dialogue.DialogueFrame.ToList()),
            //       FtpLink = _fileRef.GetFileLink(_containerName, x.Key + ".png", default) + $"?{pollShown.Where(p => p.ContentId == x.Key).FirstOrDefault().ContentUpdateDate}",
                   
            //   })
            //   .ToList();

            var jsonToReturn = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(contentInfo));

            jsonToReturn["AnswersInfo"] = answersByContent;
            jsonToReturn["PollAmount"] = pollShown.Count();
            return jsonToReturn;
        }

        public async Task<Dictionary<string, object>> Efficiency(
                                string beg, string end,
                                List<Guid?> applicationUserIds,
                                List<Guid> companyIds,
                                List<Guid> corporationIds,
                                List<Guid> deviceIds)
        {
            int active = 3;
            var role = _loginService.GetCurrentRoleName();
            var companyId = _loginService.GetCurrentCompanyId();
            var begTime = _requestFilters.GetBegDate(beg);
            var endTime = _requestFilters.GetEndDate(end);
            _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);

            var dialogueIds = GetDialogueIds(begTime, endTime, companyIds, applicationUserIds, deviceIds);
        
            var slideShowSessionsInDialogues = GetSlideShowWithDialogueIdFilteredByPoolAsync(false, dialogueIds);
            var views = slideShowSessionsInDialogues.Count();
            var clients = dialogueIds.Count();

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
                ContentFullInfo = contentsShownGroup.Where(x => x.Key2 != null && x.Key2 != "").Select(x => new ContentFullOneInfo
                {
                    ExternalLink = x.Key2.ToString(),
                    AmountViews = x.Result.Where(p => p.DialogueId != null && p.DialogueId != default(Guid)).Count(),
                    EmotionAttention = EmotionsDuringAdv(x.Result),
                    Age = x.Result.Where(p => p.DialogueId != null)?.Average(p => p.Age),
                    Male = x.Result.Where(p => p.Gender.ToLower() == "male").Count(),
                    Female = x.Result.Where(p => p.Gender.ToLower() == "female").Count(),
                    ContentType = "url"
                })
                .Union(contentsShownGroup.Where(x => x.Key1 != null).Select(x => new ContentFullOneInfo
                {
                    Content = x.Key1.ToString(),
                    AmountViews = x.Result.Where(p => p.DialogueId != null && p.DialogueId != default(Guid)).Count(),//TODO,
                    ContentName = x.Result.FirstOrDefault().ContentName,
                    EmotionAttention = EmotionsDuringAdv(x.Result),
                    Age = x.Result.Where(p => p.DialogueId != null).Average(p => p.Age),
                    Male = x.Result.Where(p => p.Gender.ToLower() == "male").Count(),
                    Female = x.Result.Where(p => p.Gender.ToLower() == "female").Count(),
                    FtpLink = _fileRef.GetFileLink(_containerName, x.Key1 + ".png", default) + $"?{x.Result.FirstOrDefault().ContentUpdateDate}",
                    ContentType = "content"
                }
                )).ToList()
            };
            var jsonToReturn = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(contentInfo));
            return jsonToReturn;
        }

        public async Task<object> Poll(
                                string beg, string end,
                                List<Guid?> applicationUserIds,
                                List<Guid> companyIds,
                                List<Guid> corporationIds,
                                List<Guid> deviceIds,
                                string type)
        {
            int active = 3;
            var role = _loginService.GetCurrentRoleName();
            var companyId = _loginService.GetCurrentCompanyId();
            var begTime = _requestFilters.GetBegDate(beg);
            var endTime = _requestFilters.GetEndDate(end);
            _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);

            var dialogues = await GetDialoguesAsync(begTime, endTime, companyIds, applicationUserIds, deviceIds);
            var slideShowSessionsInDialogues = GetSlideShowWithDialogueIdFilteredByPoolAsync(true, dialogues.Select( x=> x.DialogueId).ToList());

            List<AnswerInfo.AnswerOne> answers = await GetAnswersFullAsync(dialogues, begTime, endTime, companyIds, applicationUserIds, deviceIds);
            double conversion = GetConversion(slideShowSessionsInDialogues.Count(), answers.Count());
            List<AnswerInfo> slideShowInfoGroupByContent = slideShowSessionsInDialogues?
                .GroupBy(p => p.ContentId)
                .Select(ssh => new AnswerInfo
                {
                    Content = ssh.Key.ToString(),
                    AmountViews = ssh.Count(),
                    ContentName = ssh.FirstOrDefault().ContentName,
                    Answers = GetAnswersForOneContent(answers, ssh.Key),
                    AnswersAmount = GetAnswersForOneContent(answers, ssh.Key).Count(),
                    Conversion = (double)GetAnswersForOneContent(answers, ssh.Key).Count() / (double)ssh.Count()
                }).ToList();

            var contentInfo = new
            {
                Views = slideShowSessionsInDialogues.Count(),
                Clients = slideShowSessionsInDialogues?.Select(x => x.DialogueId).Distinct().Count(),
                Answers = slideShowInfoGroupByContent?.Sum(x => x.AnswersAmount), //answers.Count(),//
                Conversion = conversion,
                ContentFullInfo = slideShowInfoGroupByContent
            };
            if (type == "json")
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(contentInfo));

            MemoryStream excelDocStream = _utils.CreatePoolAnswersSheet(slideShowInfoGroupByContent.ToList(), $"{begTime.ToShortDateString()}_{endTime.ToShortDateString()}");
            excelDocStream.Seek(0, SeekOrigin.Begin);
            return excelDocStream;
        }


        //-----PRIVATE---
        private List<SlideShowInfo> GetSlideShowsForOneDialogueAsync(Dialogue dialogue)
        {
            var slideShows = _repository.GetAsQueryable<SlideShowSession>().Where(x => x.DialogueId == dialogue.DialogueId)
                .Select(p =>
                    new SlideShowInfo
                    {
                        BegTime = p.BegTime,
                        EndTime = p.EndTime,
                        ContentId = p.CampaignContent != null ? p.CampaignContent.ContentId : null,
                        ContentName = p.CampaignContent != null ? p.CampaignContent.Content.Name : null,
                        CampaignContentId = p.CampaignContentId,
                        ContentType = p.ContentType,
                        IsPoll = p.IsPoll,
                        Url = p.Url,
                        ApplicationUserId = (Guid)p.ApplicationUserId,
                        EmotionAttention = EmotionAttentionCalculate(dialogue.DialogueFrame.ToList())
                    })
                .ToList();
            return slideShows;
        }


        private List<SlideShowInfo> GetSlideShowWithDialogueIdFilteredByPoolAsync(
          bool isPool, List<Guid> dialogueIds
          )
        {
            if (dialogueIds.Count() == 0) return new List<SlideShowInfo>();
            var slideShows = _repository.GetAsQueryable<SlideShowSession>().Where(
                    ses => ( ses.DialogueId != null && dialogueIds.Contains((Guid)ses.DialogueId )
                    && ses.IsPoll == isPool
                    && ses.CampaignContent != null))
                .Select(ssi =>
                    new SlideShowInfo
                    {
                        BegTime = ssi.BegTime,
                        ContentId = ssi.CampaignContent.ContentId,
                        Campaign = ssi.CampaignContent.Campaign,
                        ContentType = ssi.ContentType,
                        ContentName = ssi.CampaignContent.Content != null ? ssi.CampaignContent.Content.Name : null,
                        EndTime = ssi.EndTime,
                        IsPoll = ssi.IsPoll,
                        Url = ssi.Url,
                        ApplicationUserId = ssi.ApplicationUserId,
                        DialogueId = ssi.DialogueId,
                        DialogueFrames = ssi.Dialogue.DialogueFrame.ToList(),
                        ContentUpdateDate = ssi.CampaignContent.Content.UpdateDate.ToString(),
                        Gender = ssi.Dialogue.DialogueClientProfile.Max(x => x.Gender),
                        Age = ssi.Dialogue.DialogueClientProfile.Average(x => x.Age)
                    })
                .ToList();
            return slideShows;
        }

        private async Task<List<CampaignContentAnswer>> GetAnswersInOneDialogueAsync(
            DateTime begTime, DateTime endTime, Guid deviceId)
        {
            var answers = await _repository.GetAsQueryable<CampaignContentAnswer>()
                .Where(p => p.Time >= begTime
                    && p.Time <= endTime
                    && p.DeviceId == deviceId)
                    .Include(p => p.CampaignContent)
                .ToListAsyncSafe();
            return answers;
        }

        private List<AnswerInfo.AnswerOne> GetAnswersForOneContent(List<AnswerInfo.AnswerOne> answers, Guid? contentId)
        {
            return answers.Where(x => x.ContentId == contentId).ToList();
        }

        private double GetConversion(double viewsAmount, double answersAmount)
        {
            return viewsAmount != 0 ? answersAmount / viewsAmount : 0;
        }

        private async Task<List<AnswerInfo.AnswerOne>> GetAnswersFullAsync(
                        List<Dialogue> dialogues,
                        DateTime begTime, DateTime endTime,
                        List<Guid> companyIds,
                        List<Guid?> applicationUserIds,
                        List<Guid> deviceIds)
        {
            var answers = await GetAnswersAsync(begTime, endTime, companyIds, applicationUserIds, deviceIds);

            List<AnswerInfo.AnswerOne> answersResult = new List<AnswerInfo.AnswerOne>();
            if (answers.Count() == 0 || dialogues.Count() == 0) return answersResult;
            foreach (var answ in answers)
            {
                var dialogueForAnswer = dialogues.Where(x => x.BegTime <= answ.Time
                        && x.EndTime >= answ.Time
                        && x.DeviceId == answ.DeviceId)
                    .FirstOrDefault();
                var dialogueId = dialogueForAnswer != null ? (Guid?)dialogueForAnswer.DialogueId : null;

                AnswerInfo.AnswerOne oneAnswer = new AnswerInfo.AnswerOne
                {
                    Answer = answ.Answer,
                    Time = answ.Time,
                    DialogueId = dialogueId,
                    ContentId = answ.CampaignContent?.ContentId,
                    FullName = answ.ApplicationUser?.FullName
                };
                answersResult.Add(oneAnswer);
            }
            return answersResult;
        }

        private async Task<IEnumerable<CampaignContentAnswer>> GetAnswersAsync(
                    DateTime begTime,
                    DateTime endTime,
                    List<Guid> companyIds,
                    List<Guid?> applicationUserIds,
                    List<Guid> deviceIds)
        {
            var result = await _repository.GetAsQueryable<CampaignContentAnswer>()
                                     .Include(x => x.CampaignContent)
                                     .Include(x => x.ApplicationUser)
                                     .Where(p =>
                                    p.CampaignContent != null
                                    && (p.Time >= begTime && p.Time <= endTime)
                                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                                    && (!deviceIds.Any() || deviceIds.Contains(p.DeviceId))
                                    && (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId))).ToListAsyncSafe();
            return result;
        }





        //------------------FOR CONTENT ANALYTIC------------------------
        private EmotionAttention EmotionsDuringAdv(List<SlideShowInfo> shows)
        {
            List<EmotionAttention> emotionAttentionList = new List<EmotionAttention>();
            foreach (var show in shows)
            {
                var emotionAttention = EmotionAttentionCalculate(show.BegTime, show.EndTime, show.DialogueFrames);
                if (emotionAttention != null)
                    emotionAttentionList.Add(emotionAttention);
            }
            return new EmotionAttention
            {
                Attention = emotionAttentionList.Average(x => x.Attention),
                Negative = emotionAttentionList.Average(x => x.Negative),
                Neutral = emotionAttentionList.Average(x => x.Neutral),
                Positive = emotionAttentionList.Average(x => x.Positive)
            };
        }

        private EmotionAttention EmotionDuringAdvOneDialogue(List<SlideShowInfo> shows, List<DialogueFrame> frames)
        {
            List<EmotionAttention> emotionAttentionList = new List<EmotionAttention>();
            if (frames != null && frames.Count() != 0 && shows.Count() != 0)
            {
                foreach (var show in shows)
                {
                    var emotionAttention = EmotionAttentionCalculate(show.BegTime, show.EndTime, frames);
                    if (emotionAttention != null)
                        emotionAttentionList.Add(emotionAttention);
                }
                return new EmotionAttention
                {
                    Attention = emotionAttentionList.Average(x => x.Attention),
                    Negative = emotionAttentionList.Average(x => x.Negative),
                    Neutral = emotionAttentionList.Average(x => x.Neutral),
                    Positive = emotionAttentionList.Average(x => x.Positive)
                };
            }
            return null;
        }

        //---PRIVATE---
        private EmotionAttention EmotionAttentionCalculate(DateTime begTime, DateTime endTime, List<DialogueFrame> frames)
        {
            if (frames == null || frames.Count() == 0) return null;
            //---time - advertaisment begin and end
            ReplaseFramesNullOnZero(frames);
            frames = frames.Where(x => x.Time >= begTime && x.Time <= endTime).ToList();
            if (frames?.Count() != 0)
            {
                return new EmotionAttention
                {
                    Attention = frames.Average(x => Math.Abs((decimal)x.YawShare) <= 20 ? 100 : 20),
                    Positive = frames.Average(x => x.SurpriseShare) + frames.Average(x => x.HappinessShare),
                    Negative = frames.Average(x => x.AngerShare) + frames.Average(x => x.DisgustShare) + frames.Average(x => x.FearShare) + frames.Average(x => x.SadnessShare) + frames.Average(x => x.ContemptShare),
                    Neutral = frames.Average(x => x.NeutralShare)
                };
            }
            return null;
        }

        private EmotionAttention EmotionAttentionCalculate(List<DialogueFrame> frames)
        {
            if (frames == null || frames.Count() == 0) return null;
            //---time - advertaisment begin and end
            ReplaseFramesNullOnZero(frames);
            return new EmotionAttention
            {
                Attention = frames.Average(x => Math.Abs((decimal)x.YawShare) <= 20 ? 100 : 20),
                Positive = frames.Average(x => x.SurpriseShare) + frames.Average(x => x.HappinessShare),
                Negative = frames.Average(x => x.AngerShare) + frames.Average(x => x.DisgustShare) + frames.Average(x => x.FearShare) + frames.Average(x => x.SadnessShare) + frames.Average(x => x.ContemptShare),
                Neutral = frames.Average(x => x.NeutralShare)
            };
        }

    

        private void ReplaseFramesNullOnZero(List<DialogueFrame> frames)
        {
            foreach (var item in frames)
            {
                item.YawShare = item.YawShare ?? 0;
                item.AngerShare = item.AngerShare ?? 0;
                item.ContemptShare = item.ContemptShare ?? 0;
                item.DisgustShare = item.DisgustShare ?? 0;
                item.FearShare = item.FearShare ?? 0;
                item.HappinessShare = item.HappinessShare ?? 0;
                item.NeutralShare = item.NeutralShare ?? 0;
                item.SadnessShare = item.SadnessShare ?? 0;
                item.SurpriseShare = item.SurpriseShare ?? 0;
            }
        }

        private async Task<List<Dialogue>> GetDialoguesAsync(
            DateTime begTime,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid?> applicationUserIds,
            List<Guid> deviceIds
            )
        {
            var dialogues = await _repository.GetAsQueryable<Dialogue>()
                   .Where(p => p.BegTime >= begTime
                           && p.EndTime <= endTime
                           && p.StatusId == 3
                           && p.InStatistic == true
                           && (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId))
                           && (!applicationUserIds.Any() || (p.ApplicationUserId != null && applicationUserIds.Contains((Guid)p.ApplicationUserId)))
                           && (!deviceIds.Any() || (p.DeviceId != null && deviceIds.Contains((Guid)p.DeviceId))))
                   .ToListAsyncSafe();
            return dialogues;
        }

        private List<Guid> GetDialogueIds(
         DateTime begTime,
         DateTime endTime,
         List<Guid> companyIds,
         List<Guid?> applicationUserIds,
         List<Guid> deviceIds
         )
        {
            var dialogues = _repository.GetAsQueryable<Dialogue>()
                   .Where(p => p.BegTime >= begTime
                           && p.EndTime <= endTime
                           && p.StatusId == 3
                           && p.InStatistic == true
                           && (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId))
                           && (!applicationUserIds.Any() || (p.ApplicationUserId != null && applicationUserIds.Contains((Guid)p.ApplicationUserId)))
                           && (!deviceIds.Any() || (p.DeviceId != null && deviceIds.Contains((Guid)p.DeviceId))))
                   .Select(x => x.DialogueId).ToList();
            return dialogues;
        }


        private async Task<Dialogue> GetDialogueIncludedFramesByIdAsync(Guid dialogueId)
        {
            var dialogue = await _repository.GetAsQueryable<Dialogue>()
                .Include(p => p.DialogueFrame)
                .Include(p => p.SlideShowSessions)
                .Where(p => p.DialogueId == dialogueId).FirstOrDefaultAsync();
            return dialogue;
        }
    }
}


