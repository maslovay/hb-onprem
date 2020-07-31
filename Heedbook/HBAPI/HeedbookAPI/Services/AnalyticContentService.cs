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
using HBData.Models;
using UserOperations.Models.AnalyticModels;
using HBLib.Utils;
using UserOperations.Utils.Interfaces;
using HBLib.Utils.Interfaces;
using HBData;

namespace UserOperations.Services
{
    public class AnalyticContentService
    {
        private readonly ISpreadsheetDocumentUtils _helpProvider;
        private readonly ILoginService _loginService;
        private readonly IRequestFilters _requestFilters;
        private readonly IGenericRepository _repository;
        private readonly AnalyticContentUtils _utils;
        private readonly IFileRefUtils _fileRef;
        private readonly RecordsContext _context;

        private const string _containerName = "screenshots";

        public AnalyticContentService(
            ISpreadsheetDocumentUtils helpProvider,
            ILoginService loginService,
            IRequestFilters requestFilters,
            IGenericRepository repository,
            AnalyticContentUtils utils,
            IFileRefUtils fileRef,
            RecordsContext context
            )
        {
            _helpProvider = helpProvider;
            _loginService = loginService;
            _requestFilters = requestFilters;
            _repository = repository;
            _utils = utils;
            _fileRef = fileRef;
            _context = context;
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
                .Union(contentsShownGroup.Where(x => x.Key2 == null)?.Select(x => new ContentFullOneInfo
                {
                    Content = null,
                    AmountViews = x.Result.Count(),
                    ContentType = x.Key1,
                    EmotionAttention = EmotionDuringAdvOneDialogue(x.Result, dialogue.DialogueFrame.ToList()),
                    ExternalLink = x.Key3.ToString(),
                }
                ))
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
            System.Console.WriteLine("1");
            var role = _loginService.GetCurrentRoleName();
            var companyId = _loginService.GetCurrentCompanyId();
            var begTime = _requestFilters.GetBegDate(beg);
            var endTime = _requestFilters.GetEndDate(end);
            System.Console.WriteLine("2");
            _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);

            var dialogueIds = GetDialogueIds(begTime, endTime, companyIds, applicationUserIds, deviceIds);
            System.Console.WriteLine("3");
            var slideShowSessionsInDialogues = GetSlideShowWithDialogueIdFilteredByPoolAsync(false, dialogueIds, begTime, endTime);
            System.Console.WriteLine("4");
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
            var slideShowSessionsInDialogues = GetSlideShowWithDialogueIdFilteredByPoolAsync(true, dialogues.Select( x=> x.DialogueId).ToList(),
                begTime, endTime);

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
                    Conversion = (double)GetAnswersForOneContent(answers, ssh.Key).Count() / (double)ssh.Count(),
                    FtpLink = _fileRef.GetFileLink(_containerName, ssh.FirstOrDefault().ContentId + ".png", default)
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
                        EmotionAttention = EmotionAttentionCalculate(dialogue.DialogueFrame.ToList()),
                        ContentUpdateDate = p.CampaignContent != null ? p.CampaignContent.Content.UpdateDate.ToString() : ""
                    })
                .ToList();
            return slideShows;
        }


        private List<SlideShowInfo> GetSlideShowWithDialogueIdFilteredByPoolAsync(
          bool isPool, List<Guid> dialogueIds, DateTime beg, DateTime end
          )
        {
            if (dialogueIds.Count() == 0) return new List<SlideShowInfo>();
            System.Console.WriteLine(beg);
            System.Console.WriteLine(end);

            System.Console.WriteLine("Beg");
            var dialogues = _context.Dialogues
                .Include(p => p.DialogueFrame)
                .Include(p => p.DialogueClientProfile)
                .Include(p => p.SlideShowSessions)
                .ThenInclude(p => p.CampaignContent)
                .ThenInclude(p => p.Content)
                .Where(p => p.StatusId == 3 
                    && p.InStatistic == true 
                    && p.BegTime >= beg
                    && p.EndTime < end)
                .ToList();

            var result = new List<SlideShowInfo>();
            foreach (var dialogue in dialogues)
            {
                if (dialogue.SlideShowSessions.Any())
                {
                    // foreach (var session in dialogue.SlideShowSessions.Where(p => p.CampaignContent != null))
                    // {
                    //     System.Console.WriteLine(JsonConvert.SerializeObject(dialogue.DialogueClientProfile));
                    //     System.Console.WriteLine(session.BegTime);
                    //     System.Console.WriteLine(session.CampaignContent.ContentId);
                    //     System.Console.WriteLine(session.CampaignContent.Campaign);
                    //     System.Console.WriteLine(session.ContentType);
                    //     System.Console.WriteLine(session.CampaignContent.Content != null ? session.CampaignContent.Content.Name : null);
                    //     System.Console.WriteLine(session.EndTime);
                    //     System.Console.WriteLine(session.IsPoll);
                    //     System.Console.WriteLine(session.Url);
                    //     System.Console.WriteLine(session.ApplicationUserId);
                    //     System.Console.WriteLine(session.CampaignContent.Content.UpdateDate.ToString());
                    //     System.Console.WriteLine(dialogue.DialogueClientProfile.Max(x => x.Gender));
                    //     System.Console.WriteLine(dialogue.DialogueClientProfile.Average(x => x.Age));

                    //     result.Add(new SlideShowInfo{
                    //         BegTime = session.BegTime,
                    //         ContentId = session.CampaignContent.ContentId,
                    //         Campaign = session.CampaignContent.Campaign,
                    //         ContentType = session.ContentType,
                    //         ContentName = session.CampaignContent.Content != null ? session.CampaignContent.Content.Name : null,
                    //         EndTime = session.EndTime,
                    //         IsPoll = session.IsPoll,
                    //         Url = session.Url,
                    //         ApplicationUserId = session.ApplicationUserId,
                    //         DialogueId = session.DialogueId,
                    //         DialogueFrames = dialogue.DialogueFrame.ToList(),
                    //         ContentUpdateDate = session.CampaignContent.Content.UpdateDate.ToString(),
                    //         Gender = dialogue.DialogueClientProfile.Max(x => x.Gender),
                    //         Age = dialogue.DialogueClientProfile.Average(x => x.Age)
                    //     });
                    result.AddRange(dialogue.SlideShowSessions.Select(p => new SlideShowInfo{
                        BegTime = p.BegTime,
                        ContentId = p.CampaignContent.ContentId,
                        Campaign = p.CampaignContent.Campaign,
                        ContentType = p.ContentType,
                        ContentName = p.CampaignContent.Content != null ? p.CampaignContent.Content.Name : null,
                        EndTime = p.EndTime,
                        IsPoll = p.IsPoll,
                        Url = p.Url,
                        ApplicationUserId = p.ApplicationUserId,
                        DialogueId = p.DialogueId,
                        DialogueFrames = dialogue.DialogueFrame.ToList(),
                        ContentUpdateDate = p.CampaignContent.Content.UpdateDate.ToString(),
                        Gender = dialogue.DialogueClientProfile.Max(x => x.Gender),
                        Age = dialogue.DialogueClientProfile.Average(x => x.Age)
                    }));
                    // }
                }
                System.Console.WriteLine(result.Count());
            }

            // System.Console.WriteLine("Beg1");
            // var slideShows = _repository.GetAsQueryable<SlideShowSession>().Where(
            //         ses => ses.BegTime >= beg 
            //         && ses.EndTime < end 
            //         && ses.DialogueId != null 
            //         && dialogueIds.Contains((Guid)ses.DialogueId) 
            //         && ses.IsPoll == isPool)
            //     .Select(ssi =>
            //         new SlideShowInfo
            //         {
            //             BegTime = ssi.BegTime,
            //             ContentId = ssi.CampaignContent.ContentId,
            //             Campaign = ssi.CampaignContent.Campaign,
            //             ContentType = ssi.ContentType,
            //             ContentName = ssi.CampaignContent.Content != null ? ssi.CampaignContent.Content.Name : null,
            //             EndTime = ssi.EndTime,
            //             IsPoll = ssi.IsPoll,
            //             Url = ssi.Url,
            //             ApplicationUserId = ssi.ApplicationUserId,
            //             DialogueId = ssi.DialogueId,
            //             DialogueFrames = ssi.Dialogue.DialogueFrame.ToList(),
            //             ContentUpdateDate = ssi.CampaignContent.Content.UpdateDate.ToString(),
            //             Gender = ssi.Dialogue.DialogueClientProfile.Max(x => x.Gender),
            //             Age = ssi.Dialogue.DialogueClientProfile.Average(x => x.Age)
            //         })
            //     .ToList();
            // System.Console.WriteLine("End2");
            // return slideShows;
            return result;
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


