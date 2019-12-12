using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using UserOperations.Utils;
using System.Threading.Tasks;
using UserOperations.Providers.Interfaces;
using System.IO;
using UserOperations.Models.Get.AnalyticContentController;
using HBData.Repository;
using UserOperations.Utils.AnalyticContentUtils;
using UserOperations.Controllers;
using HBData.Models;

namespace UserOperations.Services
{
    public class AnalyticContentService
    {
        private readonly IHelpProvider _helpProvider;
        private readonly ILoginService _loginService;
        private readonly IRequestFilters _requestFilters;
        private readonly IGenericRepository _repository;
        private readonly AnalyticContentUtils _utils;

        public AnalyticContentService(
            IHelpProvider helpProvider,
            ILoginService loginService,
            IRequestFilters requestFilters,
            IGenericRepository repository,
            AnalyticContentUtils utils
            )
        {
            _helpProvider = helpProvider;
            _loginService = loginService;
            _requestFilters = requestFilters;
            _repository = repository;
            _utils = utils;
        }

//---FOR ONE DIALOGUE---
        public async Task<Dictionary<string, object>> ContentShows( Guid dialogueId )
        {
                var dialogue = await GetDialogueIncludedFramesByIdAsync(dialogueId);
                if (dialogue == null) throw new NoFoundException("No such dialogue");

                var slideShowSessionsAll = await GetSlideShowsForOneDialogueAsync(dialogue);

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
                        EmotionAttention = EmotionDuringAdvOneDialogue(x.Result, dialogue.DialogueFrame.ToList())
                    })
                    .Union(contentsShownGroup.Where(x => x.Key2 == null).Select(x => new ContentOneInfo
                    {
                        Content = x.Key3,
                        AmountOne = x.Result.Count(),
                        ContentType = x.Key1,
                        EmotionAttention = EmotionDuringAdvOneDialogue(x.Result, dialogue.DialogueFrame.ToList()),
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
                var answers = await GetAnswersInOneDialogueAsync(slideShowSessionsAll, dialogue.BegTime, dialogue.EndTime, dialogue.ApplicationUserId);
                  
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
                            EmotionAttention = EmotionDuringAdvOneDialogue(x.Result, dialogue.DialogueFrame.ToList()),
                        })
                    .ToList();

                var jsonToReturn = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(contentInfo));

                jsonToReturn["AnswersInfo"] = answersByContent;
                jsonToReturn["AnswersAmount"] = slideShowSessionsAll.Where(p => p.IsPoll).Count();
                return jsonToReturn;
        }

        public async Task<Dictionary<string, object>> Efficiency(
                                string beg, string end,
                                List<Guid> applicationUserIds,
                                List<Guid> companyIds,
                                List<Guid> corporationIds,
                                List<Guid> workerTypeIds )
        {
                var role = _loginService.GetCurrentRoleName();
                var companyId = _loginService.GetCurrentCompanyId();
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);

                var dialogues = await GetDialoguesInfoWithFramesAsync(begTime, endTime, companyIds, applicationUserIds, workerTypeIds);
                //var slideShowSessionsAll = await _analyticContentProvider.GetSlideShowFilteredByPoolAsync(begTime, endTime, companyIds, applicationUserIds, workerTypeIds, false);
               
                //foreach ( var session in slideShowSessionsAll )
                //{
                //    var dialog = dialogues.FirstOrDefault(x => x.BegTime <= session.BegTime && x.EndTime >= session.BegTime && x.ApplicationUserId == session.ApplicationUserId);
                //    session.DialogueId = dialog?.DialogueId;
                //    session.DialogueFrames = dialog?.DialogueFrame;
                //    session.Age = dialog?.Age;
                //    session.Gender = dialog?.Gender;
                //}
                var slideShowSessionsInDialogues = await GetSlideShowWithDialogueIdFilteredByPoolAsync(begTime, endTime, companyIds, applicationUserIds, workerTypeIds, false, dialogues);
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
                        EmotionAttention = EmotionsDuringAdv(x.Result),
                        Age = x.Result.Where(p => p.DialogueId != null).Average(p => p.Age),
                        Male = x.Result.Where(p => p.Gender == "male").Count(),
                        Female = x.Result.Where(p => p.Gender == "female").Count()
                    })
                    .Union(contentsShownGroup.Where(x => x.Key2 == null).Select(x => new ContentFullOneInfo
                    {
                        Content = x.Key1.ToString(),
                        AmountViews = x.Result.Where(p => p.DialogueId != null && p.DialogueId != default(Guid)).Count(),//TODO,
                        ContentName = x.Result.FirstOrDefault().ContentName,  
                        EmotionAttention = EmotionsDuringAdv(x.Result),
                        Age = x.Result.Where(p => p.DialogueId != null).Average(p => p.Age),
                        Male = x.Result.Where(p => p.Gender == "male").Count(),
                        Female = x.Result.Where(p => p.Gender == "female").Count()
                    }
                    )).ToList()
                };
                var jsonToReturn = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(contentInfo));
                return jsonToReturn;
        }

        public async Task<object> Poll(
                                string beg, string end,
                                List<Guid> applicationUserIds,
                                List<Guid> companyIds,
                                List<Guid> corporationIds,
                                List<Guid> workerTypeIds,
                                string type)
        {
                var role = _loginService.GetCurrentRoleName();
                var companyId = _loginService.GetCurrentCompanyId();
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);

                var dialogues = await GetDialogueInfos(begTime, endTime, companyIds, applicationUserIds, workerTypeIds);
                var slideShowSessionsAll = await GetSlideShowWithDialogueIdFilteredByPoolAsync(begTime, endTime, companyIds, applicationUserIds, workerTypeIds, true, dialogues);
                var answers = await GetAnswersFullAsync(slideShowSessionsAll, begTime, endTime, companyIds, applicationUserIds, workerTypeIds);

                double conversion = GetConversion(slideShowSessionsAll.Count(), answers.Count());

                var slideShowInfoGroupByContent = slideShowSessionsAll
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
                    Views = slideShowSessionsAll.Count(),
                    Clients = slideShowSessionsAll.Select(x => x.DialogueId).Distinct().Count(),
                    Answers = slideShowInfoGroupByContent.Sum(x => x.AnswersAmount), //answers.Count(),//                    
                    Conversion = conversion,
                    ContentFullInfo = slideShowInfoGroupByContent
                };
            if(type == "json")
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(contentInfo));           

            MemoryStream excelDocStream = _utils.CreatePoolAnswersSheet(slideShowInfoGroupByContent.ToList(), $"{begTime.ToShortDateString()}_{endTime.ToShortDateString()}");
            excelDocStream.Seek(0, SeekOrigin.Begin);
            return excelDocStream;
        }


        //public async Task<MemoryStream> PollFile(
        //                        string beg, string end, 
        //                        List<Guid> applicationUserIds,
        //                        List<Guid> companyIds,
        //                        List<Guid> corporationIds,
        //                        List<Guid> workerTypeIds )
        //{
        //    var role = _loginService.GetCurrentRoleName();
        //    var companyId = _loginService.GetCurrentCompanyId();
        //    var begTime = _requestFilters.GetBegDate(beg);
        //    var endTime = _requestFilters.GetEndDate(end);
        //    _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);

        //    var dialogues = await GetDialogueInfos(begTime, endTime, companyIds, applicationUserIds, workerTypeIds);
        //    var slideShowSessionsAll = await GetSlideShowWithDialogueIdFilteredByPoolAsync(begTime, endTime, companyIds, applicationUserIds, workerTypeIds, true, dialogues);
        //    var answers = await GetAnswersFullAsync(slideShowSessionsAll, begTime, endTime, companyIds, applicationUserIds, workerTypeIds);

        //    double conversion = GetConversion(slideShowSessionsAll.Count(), answers.Count());

        //    var slideShowInfoGroupByContent = slideShowSessionsAll
        //        .GroupBy(p => p.ContentId)
        //        .Select(ssh => new AnswerInfo
        //        {
        //            Content = ssh.Key.ToString(),
        //            AmountViews = ssh.Count(),
        //            ContentName = ssh.FirstOrDefault().ContentName,
        //            Answers = GetAnswersForOneContent(answers, ssh.Key),
        //            AnswersAmount = GetAnswersForOneContent(answers, ssh.Key).Count(),
        //            Conversion = (double)GetAnswersForOneContent(answers, ssh.Key).Count() / (double)ssh.Count()
        //        }).ToList();

        //    var contentInfo = new
        //    {
        //        Views = slideShowSessionsAll.Count(),
        //        Clients = slideShowSessionsAll.Select(x => x.DialogueId).Distinct().Count(),
        //        Answers = slideShowInfoGroupByContent.Sum(x => x.AnswersAmount), //answers.Count(),//                    
        //        Conversion = conversion,
        //        ContentFullInfo = slideShowInfoGroupByContent
        //    };
        //        MemoryStream excelDocStream = _utils.CreatePoolAnswersSheet(slideShowInfoGroupByContent.ToList(), $"{begTime.ToShortDateString()}_{endTime.ToShortDateString()}");
        //        excelDocStream.Seek(0, SeekOrigin.Begin);
        //    return excelDocStream;
        //}




        //-----PRIVATE---
        private async Task<List<SlideShowInfo>> GetSlideShowsForOneDialogueAsync(Dialogue dialogue)
        {
            var slideShows = await _repository.GetAsQueryable<SlideShowSession>()
                .Where(p => p.BegTime >= dialogue.BegTime
                    && p.BegTime <= dialogue.EndTime
                    && p.ApplicationUserId == dialogue.ApplicationUserId)
                .Select(p =>
                    new SlideShowInfo
                    {
                        BegTime = p.BegTime,
                        ContentId = p.CampaignContent != null ? p.CampaignContent.ContentId : null,
                        ContentName = p.CampaignContent != null ? p.CampaignContent.Content.Name : null,
                        CampaignContentId = p.CampaignContentId,
                        ContentType = p.ContentType,
                        EndTime = p.EndTime,
                        IsPoll = p.IsPoll,
                        Url = p.Url,
                        ApplicationUserId = (Guid)p.ApplicationUserId,
                        EmotionAttention = EmotionAttentionCalculate(p.BegTime, p.EndTime, dialogue.DialogueFrame.ToList())
                    })
                .ToListAsyncSafe();
            return slideShows;
        }
        
        private async Task<List<SlideShowInfo>> GetSlideShowWithDialogueIdFilteredByPoolAsync(
           DateTime begTime,
           DateTime endTime,
           List<Guid> companyIds,
           List<Guid> applicationUserIds,
           List<Guid> workerTypeIds,
           bool isPool,
           List<DialogueInfoWithFrames> dialogues
           )
        {
            var slideShows = await _repository.GetAsQueryable<SlideShowSession>()
                .Where(p => p.IsPoll == isPool
                    && p.BegTime >= begTime
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
                        Campaign = p.CampaignContent.Campaign,
                        ContentType = p.ContentType,
                        ContentName = p.CampaignContent.Content != null ? p.CampaignContent.Content.Name : null,
                        EndTime = p.EndTime,
                        IsPoll = p.IsPoll,
                        Url = p.Url,
                        ApplicationUserId = (Guid)p.ApplicationUserId,
                        DialogueId = dialogues.FirstOrDefault(x => x.BegTime <= p.BegTime
                                && x.EndTime >= p.BegTime
                                && x.ApplicationUserId == p.ApplicationUserId)
                            .DialogueId,
                        DialogueFrames = dialogues.FirstOrDefault(x => x.BegTime <= p.BegTime
                                && x.EndTime >= p.BegTime
                                && x.ApplicationUserId == p.ApplicationUserId)
                            .DialogueFrame,
                        Age = dialogues.FirstOrDefault(x => x.BegTime <= p.BegTime
                                && x.EndTime >= p.BegTime
                                && x.ApplicationUserId == p.ApplicationUserId)
                            .Age,
                        Gender = dialogues.FirstOrDefault(x => x.BegTime <= p.BegTime
                                && x.EndTime >= p.BegTime
                                && x.ApplicationUserId == p.ApplicationUserId)
                            .Gender
                    })
                .Where(x => x.DialogueId != null && x.DialogueId != default(Guid))
                .ToListAsyncSafe();
            return slideShows;
        }

        private async Task<List<SlideShowInfo>> GetSlideShowWithDialogueIdFilteredByPoolAsync(
          DateTime begTime,
          DateTime endTime,
          List<Guid> companyIds,
          List<Guid> applicationUserIds,
          List<Guid> workerTypeIds,
          bool isPool,
          List<DialogueInfo> dialogues
          )
        {
            var slideShows = await _repository.GetAsQueryable<SlideShowSession>()
                .Where(p => p.IsPoll == isPool
                    && p.BegTime >= begTime
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
                        Campaign = p.CampaignContent.Campaign,
                        ContentType = p.ContentType,
                        ContentName = p.CampaignContent.Content != null ? p.CampaignContent.Content.Name : null,
                        EndTime = p.EndTime,
                        IsPoll = p.IsPoll,
                        Url = p.Url,
                        ApplicationUserId = (Guid)p.ApplicationUserId,
                        DialogueId = dialogues.FirstOrDefault(x => x.BegTime <= p.BegTime
                                && x.EndTime >= p.BegTime
                                && x.ApplicationUserId == p.ApplicationUserId)
                            .DialogueId
                    })
                .Where(x => x.DialogueId != null && x.DialogueId != default(Guid))
                .ToListAsyncSafe();
            return slideShows;
        }

        private async Task<List<CampaignContentAnswer>> GetAnswersInOneDialogueAsync(List<SlideShowInfo> slideShowInfos, DateTime begTime, DateTime endTime, Guid applicationUserId)
        {
            var answers = await _repository.GetAsQueryable<CampaignContentAnswer>()
                .Where(p => slideShowInfos
                    .Select(x => x.CampaignContentId)
                    .Distinct()
                    .Contains(p.CampaignContentId)
                    && p.Time >= begTime
                    && p.Time <= endTime
                    && p.ApplicationUserId == applicationUserId)
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

        private async Task<List<AnswerInfo.AnswerOne>> GetAnswersFullAsync(List<SlideShowInfo> slideShowSessionsAll, DateTime begTime, DateTime endTime, List<Guid> companyIds, List<Guid> applicationUserIds, List<Guid> workerTypeIds)
        {
            var answers = await GetAnswersAsync(begTime, endTime, companyIds, applicationUserIds, workerTypeIds);

            List<AnswerInfo.AnswerOne> answersResult = new List<AnswerInfo.AnswerOne>();
            foreach (var answ in answers)
            {
                var slideShowSessionForAnswer = slideShowSessionsAll.Where(x => x.BegTime <= answ.Time
                        && x.EndTime >= answ.Time
                        && x.ApplicationUserId == answ.ApplicationUserId)
                    .FirstOrDefault();
                var dialogueId = slideShowSessionForAnswer != null ? slideShowSessionForAnswer.DialogueId : null;

                AnswerInfo.AnswerOne oneAnswer = new AnswerInfo.AnswerOne
                {
                    Answer = answ.Answer,
                    Time = answ.Time,
                    DialogueId = dialogueId,
                    ContentId = answ.CampaignContent?.ContentId
                };
                answersResult.Add(oneAnswer);
            }
            return answersResult;
        }

        private List<SlideShowInfo> AddDialogueIdToShow(List<SlideShowInfo> slideShowSessionsAll, List<DialogueInfoWithFrames> dialogues)
        {
            foreach (var session in slideShowSessionsAll)
            {
                var dialog = dialogues.Where(x => x.BegTime <= session.BegTime && x.EndTime >= session.BegTime)
                        .FirstOrDefault();
                session.DialogueId = dialog?.DialogueId;
            }
            slideShowSessionsAll = slideShowSessionsAll.Where(x => x.DialogueId != null && x.DialogueId != default(Guid)).ToList();
            return slideShowSessionsAll;
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
            if (frames != null && frames.Count() != 0)
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

        private async Task<IEnumerable<CampaignContentAnswer>> GetAnswersAsync(DateTime begTime, DateTime endTime, List<Guid> companyIds, List<Guid> applicationUserIds, List<Guid> workerTypeIds)
        {
            var result = await _repository.GetAsQueryable<CampaignContentAnswer>()
                                     .Include(x => x.CampaignContent)
                                     .Where(p =>
                                    p.CampaignContent != null
                                    && (p.Time >= begTime && p.Time <= endTime)
                                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                                    && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId))
                                    && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))).ToListAsyncSafe();
            return result;
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

        private async Task<List<DialogueInfoWithFrames>> GetDialoguesInfoWithFramesAsync(
            DateTime begTime,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid> applicationUserIds,
            List<Guid> workerTypeIds
            )
        {
            var dialogues = await _repository.GetAsQueryable<Dialogue>()
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
                   .ToListAsyncSafe();
            return dialogues;
        }

        private async Task<List<DialogueInfo>> GetDialogueInfos(
            DateTime begTime,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid> applicationUserIds,
            List<Guid> workerTypeIds)
        {
            return await _repository.GetAsQueryable<Dialogue>()
                .Include(p => p.ApplicationUser)
                .Include(p => p.DialogueClientSatisfaction)
                .Where(p => p.BegTime >= begTime
                    && p.EndTime <= endTime
                    && p.StatusId == 3
                    && p.InStatistic == true
                    && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                    && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)))
                .Select(p => new DialogueInfo
                {
                    DialogueId = p.DialogueId,
                    ApplicationUserId = p.ApplicationUserId,
                    BegTime = p.BegTime,
                    EndTime = p.EndTime,
                    SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal
                })
                .ToListAsyncSafe();
        }

        private async Task<Dialogue> GetDialogueIncludedFramesByIdAsync(Guid dialogueId)
        {
            var dialogue = await _repository.GetAsQueryable<Dialogue>()
                .Include(p => p.DialogueFrame)
                .Where(p => p.DialogueId == dialogueId).FirstOrDefaultAsync();
            return dialogue;
        }
    }
}

 
