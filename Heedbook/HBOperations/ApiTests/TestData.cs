using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Models.AccountViewModels;
using HBData.Repository;
using Microsoft.Extensions.Configuration;
using Moq;
using UserOperations.AccountModels;
using UserOperations.Models.AnalyticModels;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System.Linq.Expressions;
using System.Threading;
using UserOperations.Models.Get;
using UserOperations.Models.Get.HomeController;

namespace ApiTests
{
    public static class TestData
    {
        internal static string beg, end;
        internal static DateTime begDate, endDate, prevDate;
        internal static string token;
        internal static Dictionary<string, string> tokenclaims;
        internal static List<Guid> companyIds;
        internal static string email;

        private static Guid industryId = Guid.Parse("99960395-2cc3-46e8-bcef-c844f1048999");
        private static Guid userId = Guid.Parse("8d5cd62c-2ea0-406e-8ec1-a544d048a9d0");
        private static string companyName = "TEST Co";
        private static string fullName = "test1@heedbook.com";

        /// <summary>
        /// SESSIONS
        /// </summary>
        /// <returns></returns>
        internal static async Task<IEnumerable<SessionInfo>> GetSessions()
        {
            return new List<SessionInfo>
            {
                new SessionInfo
                {
                    ApplicationUserId = userId,
                    BegTime = new DateTime(2019,10,04, 12, 19,00),
                    EndTime = new DateTime(2019,10,04,12,20,25),
                    CompanyId = GetCompanyIds().First(),
                    FullName = fullName,
                    IndustryId = industryId
                },
                  new SessionInfo
                {
                    ApplicationUserId = userId,
                    BegTime = new DateTime(2019,10,04, 18, 19,00),
                    EndTime = new DateTime(2019,10,04,18,25,30),
                    CompanyId = GetCompanyIds().First(),
                    FullName = fullName,
                    IndustryId = industryId
                }
            };
        }
        internal static async Task<IEnumerable<SessionInfo>> GetEmptySessions()
        {
            return null;
        }

        /// <summary>
        /// DIALOGUES
        /// </summary>
        /// <returns></returns>
        internal static IQueryable<Dialogue> GetDialoguesWithUserPhrasesSatisfaction()
        {
            var dialogues = new List<Dialogue>
            {
                new Dialogue
                {
                    ApplicationUserId = userId,
                    BegTime = new DateTime(2019,10,04, 12, 19,00),
                    EndTime = new DateTime(2019,10,04,12,20,25),
                    CreationTime = new DateTime(2019,10,04, 12, 19,00),
                    InStatistic = true,
                    StatusId = 3,
                    PersonId = Guid.Parse("1d1cd12c-2ea0-406e-8ec1-a544d018a1d1"),
                    DialogueId = Guid.Parse("2d2cd22c-2ea0-406e-8ec1-a544d012a2d2"),
                    LanguageId = 2                    ,
                    DialoguePhrase = new List<DialoguePhrase>
                    {
                       GetRandomDialoguePhraseIncluded(Guid.Parse("2d2cd22c-2ea0-406e-8ec1-a544d012a2d2"))
                    },
                   ApplicationUser = User1(),
                   DialogueClientSatisfaction = new List<DialogueClientSatisfaction>
                   {
                       new DialogueClientSatisfaction
                       {
                           MeetingExpectationsTotal = 0.45,
                           BegMoodByNN = 0.4,
                           EndMoodByNN = 0.7
                       }
                   }
                },
                  new Dialogue
                {
                    ApplicationUserId = userId,
                    BegTime = new DateTime(2019,10,04, 18, 19,00),
                    EndTime = new DateTime(2019,10,04,18,25,30),
                    CreationTime = new DateTime(2019,10,04, 19, 19,00),
                    InStatistic = true,
                    StatusId = 3,
                    PersonId = Guid.Parse("3d3cd13c-2ea0-406e-8ec1-a544d018a333"),
                    DialogueId = Guid.Parse("4d4cd44c-2ea0-406e-8ec1-a544d012a3d3"),
                    LanguageId = 2,
                    DialoguePhrase = new List<DialoguePhrase>
                    {
                       GetRandomDialoguePhraseIncluded(Guid.Parse("4d4cd44c-2ea0-406e-8ec1-a544d012a3d3"))
                    },
                   ApplicationUser = GetUsersCompany1().First(),
                   DialogueClientSatisfaction = new List<DialogueClientSatisfaction>
                   {
                       new DialogueClientSatisfaction
                       {
                           MeetingExpectationsTotal = 0.5,
                           BegMoodByNN = 0.5,
                           EndMoodByNN = 0.6
                       }
                   }
                }
            };
            return dialogues.AsQueryable();
        }

        internal static IQueryable<CampaignContentAnswer> GetCampaignContentsAnswers()
        {
            return new List<CampaignContentAnswer>{
                new CampaignContentAnswer
                {
                    Answer = "5",
                    ApplicationUser = User1(),
                    ApplicationUserId = User1().Id,
                    CampaignContent = GetCampaignContents().First(),
                    CampaignContentAnswerId = Guid.NewGuid(),
                    CampaignContentId = GetCampaignContents().First().CampaignContentId,
                    Time = begDate.AddMinutes(1)
                },
                 new CampaignContentAnswer
                {
                    Answer = "9",
                    ApplicationUser = User1(),
                    ApplicationUserId = User1().Id,
                    CampaignContent = GetCampaignContents().First(),
                    CampaignContentAnswerId = Guid.NewGuid(),
                    CampaignContentId = GetCampaignContents().First().CampaignContentId,
                    Time = begDate.AddDays(5)//!!!
                },
                  new CampaignContentAnswer
                {
                    Answer = "2",
                    ApplicationUser = User2(),//!!!
                    ApplicationUserId = User2().Id,
                    CampaignContent = GetCampaignContents().First(),
                    CampaignContentAnswerId = Guid.NewGuid(),
                    CampaignContentId = GetCampaignContents().First().CampaignContentId,
                    Time = begDate.AddMinutes(1)
                },
                   new CampaignContentAnswer
                {
                    Answer = "5",
                    ApplicationUser = User3(),//!!!
                    ApplicationUserId = User3().Id,
                    CampaignContent = GetCampaignContents().First(),
                    CampaignContentAnswerId = Guid.NewGuid(),
                    CampaignContentId = GetCampaignContents().First().CampaignContentId,
                    Time = begDate.AddMinutes(1)
                }
            }.AsQueryable();
        }

        internal static IQueryable<Dialogue> GetDialoguesWithFrames()
        {
            var dialogues = new List<Dialogue>
            {
                new Dialogue
                {
                    ApplicationUserId = userId,
                    BegTime = begDate.AddMinutes(1),
                    EndTime = begDate.AddMinutes(3),
                    CreationTime = new DateTime(2019,10,04, 12, 19,00),
                    InStatistic = true,
                    StatusId = 3,
                    PersonId = Guid.Parse("1d1cd12c-2ea0-406e-8ec1-a544d018a1d1"),
                    DialogueId = Guid.Parse("2d2cd22c-2ea0-406e-8ec1-a544d012a2d2"),
                    LanguageId = 2                    ,
                    DialogueFrame = new List<DialogueFrame>
                    {
                        new DialogueFrame
                        {
                            DialogueId = Guid.Parse("2d2cd22c-2ea0-406e-8ec1-a544d012a2d2"),
                            DialogueFrameId = Guid.Parse("5d5cd55c-5ea0-406e-8ec1-a544d012a2d2"),
                            IsClient = true,
                            Time = begDate.AddMinutes(2),
                            AngerShare =  0.3,
                            ContemptShare = 0,
                            DisgustShare = 0.3,
                            FearShare = 0,
                            HappinessShare = 0.1,
                            NeutralShare = 0.1,
                            SadnessShare = 0.1,
                            SurpriseShare = 0.2,
                            YawShare = 20
        }
                    },
                   ApplicationUser = User1(),
                   DialogueClientSatisfaction = new List<DialogueClientSatisfaction>
                   {
                       new DialogueClientSatisfaction
                       {
                           MeetingExpectationsTotal = 0.45,
                           BegMoodByNN = 0.4,
                           EndMoodByNN = 0.7
                       }
                   }
                },
                  new Dialogue
                {
                    ApplicationUserId = userId,
                    BegTime = begDate.AddMinutes(3),
                    EndTime = begDate.AddMinutes(5),
                    CreationTime = new DateTime(2019,10,04, 19, 19,00),
                    InStatistic = true,
                    StatusId = 3,
                    PersonId = Guid.Parse("3d3cd13c-2ea0-406e-8ec1-a544d018a333"),
                    DialogueId = Guid.Parse("4d4cd44c-2ea0-406e-8ec1-a544d012a3d3"),
                    LanguageId = 2,
                    DialogueFrame = new List<DialogueFrame>
                    {
                        new DialogueFrame
                        {
                            DialogueId = Guid.Parse("4d4cd44c-2ea0-406e-8ec1-a544d012a3d3"),
                            DialogueFrameId = Guid.Parse("5d5cd55c-5ea0-406e-8ec1-a544d012a2d3"),
                            IsClient = true,
                            Time = begDate.AddMinutes(4),
                            AngerShare =  0.2,
                            ContemptShare = 0.1,
                            DisgustShare = 0.3,
                            FearShare = 0,
                            HappinessShare = 0.1,
                            NeutralShare = 0.1,
                            SadnessShare = 0.1,
                            SurpriseShare = 0.2,
                            YawShare = 20
        }
                    },
                    ApplicationUser = GetUsersCompany1().First(),
                }
            };
            return dialogues.AsQueryable();
        }
        internal static IQueryable<Dialogue> GetDialoguesSimple()
        {
            return new List<Dialogue>()
                {
                    new Dialogue
                    {
                        DialogueId = Guid.NewGuid(),
                        PersonId = Guid.NewGuid(),
                        DialogueClientProfile = new List<DialogueClientProfile>(){new DialogueClientProfile{Age = 20, Gender = "male"}}
                    },
                    new Dialogue
                    {
                        DialogueId = Guid.NewGuid(),
                        PersonId = Guid.NewGuid(),
                        DialogueClientProfile = new List<DialogueClientProfile>(){new DialogueClientProfile{Age = 25, Gender = "female"}}
                    },
                }
                .AsQueryable();
        }
        internal static IQueryable<Dialogue> GetEmptyDialogues()
        {
            return new List<Dialogue>().AsQueryable();
        }
        internal static List<DialogueInfoFull> GetDialogueInfo()
        {
            return new List<DialogueInfoFull>()
            {
                new DialogueInfoFull(){BegTime = new DateTime(2019, 10, 29, 18, 30, 00), EndTime = new DateTime(2019, 10, 29, 19, 00, 00), ApplicationUserId = Guid.Parse("2c88e4b6-f5af-49ce-95db-171514607361")},
                new DialogueInfoFull(){BegTime = new DateTime(2019, 10, 30, 18, 30, 00), EndTime = new DateTime(2019, 10, 30, 19, 00, 00), ApplicationUserId = Guid.Parse("2c88e4b6-f5af-49ce-95db-171514607362")},
                new DialogueInfoFull(){BegTime = new DateTime(2019, 10, 30, 19, 10, 00), EndTime = new DateTime(2019, 10, 30, 19, 40, 00), ApplicationUserId = Guid.Parse("2c88e4b6-f5af-49ce-95db-171514607363")},
                new DialogueInfoFull(){BegTime = new DateTime(2019, 10, 30, 19, 50, 00), EndTime = new DateTime(2019, 10, 30, 20, 20, 00), ApplicationUserId = Guid.Parse("2c88e4b6-f5af-49ce-95db-171514607364")},
                new DialogueInfoFull(){BegTime = new DateTime(2019, 10, 30, 19, 10, 00), EndTime = new DateTime(2019, 10, 30, 19, 40, 00), ApplicationUserId = Guid.Parse("2c88e4b6-f5af-49ce-95db-171514607365")},
                new DialogueInfoFull(){BegTime = new DateTime(2019, 10, 30, 20, 30, 00), EndTime = new DateTime(2019, 10, 30, 21, 00, 00), ApplicationUserId = Guid.Parse("2c88e4b6-f5af-49ce-95db-171514607366")},
                new DialogueInfoFull(){BegTime = new DateTime(2019, 10, 30, 21, 10, 00), EndTime = new DateTime(2019, 10, 30, 21, 40, 00), ApplicationUserId = Guid.Parse("2c88e4b6-f5af-49ce-95db-171514607367")},
                new DialogueInfoFull(){BegTime = new DateTime(2019, 10, 31, 18, 30, 00), EndTime = new DateTime(2019, 10, 31, 19, 00, 00), ApplicationUserId = Guid.Parse("2c88e4b6-f5af-49ce-95db-171514607368")},
                new DialogueInfoFull(){BegTime = new DateTime(2019, 11, 01, 18, 30, 00), EndTime = new DateTime(2019, 11, 01, 19, 00, 00), ApplicationUserId = Guid.Parse("2c88e4b6-f5af-49ce-95db-171514607369")}
            };
        }
        internal static IQueryable<UserOperations.Models.AnalyticModels.DialogueInfoWithFrames> GetDialogueInfoWithFrames()
        {
            var dialogues = new List<UserOperations.Models.AnalyticModels.DialogueInfoWithFrames>
            {
                new UserOperations.Models.AnalyticModels.DialogueInfoWithFrames
                {
                    ApplicationUserId = userId,
                    BegTime = begDate,
                    EndTime = begDate.AddMinutes(3),
                    DialogueId = Guid.Parse("2d2cd22c-2ea0-406e-8ec1-a544d012a2d2"),
                    DialogueFrame = new List<DialogueFrame>
                    {
                        new DialogueFrame
                        {
                            DialogueId = Guid.Parse("2d2cd22c-2ea0-406e-8ec1-a544d012a2d2"),
                            DialogueFrameId = Guid.Parse("5d5cd55c-5ea0-406e-8ec1-a544d012a2d2"),
                            IsClient = true,
                            Time = begDate.AddMinutes(1),
                            AngerShare =  0.2,
                            HappinessShare = 0.3,
                            NeutralShare = 0.5,
                            YawShare = 18
                    },
                         new DialogueFrame
                        {
                            DialogueId = Guid.Parse("2d2cd22c-2ea0-406e-8ec1-a544d012a2d2"),
                            DialogueFrameId = Guid.Parse("5d5cd55c-5ea0-406e-8ec1-a544d012a2d2"),
                            IsClient = true,
                            Time = begDate.AddMinutes(1.5),
                            AngerShare =  0.4,
                            HappinessShare = 0.5,
                            NeutralShare = 0.3,
                            YawShare = 80
                    }
                    },
                },
                  new UserOperations.Models.AnalyticModels.DialogueInfoWithFrames
                {
                    ApplicationUserId = userId,
                   BegTime = begDate.AddMinutes(3),
                    EndTime = begDate.AddMinutes(5),
                    DialogueId = Guid.Parse("4d4cd44c-2ea0-406e-8ec1-a544d012a3d3"),
                    DialogueFrame = new List<DialogueFrame>
                    {
                        new DialogueFrame
                        {
                            DialogueId = Guid.Parse("4d4cd44c-2ea0-406e-8ec1-a544d012a3d3"),
                            DialogueFrameId = Guid.Parse("5d5cd55c-5ea0-406e-8ec1-a544d012a2d3"),
                            IsClient = true,
                            Time = begDate.AddMinutes(5),
                            AngerShare =  0.2,
                            ContemptShare = 0.8,
                            YawShare = 90        }
                    }
                }
            };
            return dialogues.AsQueryable();
        }
        public static IQueryable<Dialogue> GetDialoguesIncluded()
        {
            var dialogues = new List<Dialogue>
            {
                new Dialogue
                {
                    ApplicationUserId = userId,
                    BegTime = new DateTime(2019,10,04, 12, 19,00),
                    EndTime = new DateTime(2019,10,04,12,20,25),
                    CreationTime = new DateTime(2019,10,04, 12, 19,00),
                    InStatistic = true,
                    StatusId = 3,
                    PersonId = Guid.Parse("1d1cd12c-2ea0-406e-8ec1-a544d018a1d1"),
                    DialogueId = Guid.Parse("2d2cd22c-2ea0-406e-8ec1-a544d012a2d2"),
                    LanguageId = 2                    ,
                    DialoguePhrase = new List<DialoguePhrase>
                    {
                       GetRandomDialoguePhraseIncluded(Guid.Parse("2d2cd22c-2ea0-406e-8ec1-a544d012a2d2"))
                    },
                   ApplicationUser = User1(),
                   DialogueClientSatisfaction = new List<DialogueClientSatisfaction>
                   {
                       new DialogueClientSatisfaction
                       {
                           MeetingExpectationsTotal = 0.45,
                           BegMoodByNN = 0.4,
                           EndMoodByNN = 0.7
                       }
                   }
                },
                  new Dialogue
                {
                    ApplicationUserId = userId,
                    BegTime = new DateTime(2019,10,04, 18, 19,00),
                    EndTime = new DateTime(2019,10,04,18,25,30),
                    CreationTime = new DateTime(2019,10,04, 19, 19,00),
                    InStatistic = true,
                    StatusId = 3,
                    PersonId = Guid.Parse("3d3cd13c-2ea0-406e-8ec1-a544d018a333"),
                    DialogueId = Guid.Parse("4d4cd44c-2ea0-406e-8ec1-a544d012a3d3"),
                    LanguageId = 2,
                    DialoguePhrase = new List<DialoguePhrase>
                    {
                       GetRandomDialoguePhraseIncluded(Guid.Parse("4d4cd44c-2ea0-406e-8ec1-a544d012a3d3"))
                    },
                   ApplicationUser = User1(),
                   DialogueClientSatisfaction = new List<DialogueClientSatisfaction>
                   {
                       new DialogueClientSatisfaction
                       {
                           MeetingExpectationsTotal = 0.5,
                           BegMoodByNN = 0.5,
                           EndMoodByNN = 0.6
                       }
                   }
                }
            };
            return dialogues.AsQueryable();
        }

        private static DialoguePhrase GetRandomDialoguePhraseIncluded(Guid dialogueId)
        {
            return new DialoguePhrase
            {
                DialogueId = dialogueId,
                DialoguePhraseId = Guid.Parse("5d5cd55c-5ea0-406e-8ec1-a544d012a2d2"),
                IsClient = true,
                PhraseId = Guid.Parse("6d6cd66c-6ea0-606e-8ec1-a544d012a2d2"),
                PhraseTypeId = Guid.Parse("55560395-2cc3-46e8-bcef-c844f1048555")//cross
            };
        }
        internal static async Task<Guid> GetCrossPhraseId()
        {
            return Guid.Parse("55560395-2cc3-46e8-bcef-c844f1048555");
        }


        /// <summary>
        /// CONTENT, CAMPAIGN, SESSIONS
        /// </summary>
        /// <returns></returns>
        /// 
        internal static IQueryable<SlideShowSession> GetSlideShowSessions()
        {
            return new List<SlideShowSession>
            {
                new SlideShowSession
                {
                    ApplicationUserId = userId,
                    BegTime = begDate.AddMinutes(1),
                    EndTime = begDate.AddMinutes(2),
                    CampaignContentId = GetCampaignContents().First().CampaignContentId,
                    IsPoll = false,
                    SlideShowSessionId = Guid.Parse("6d5cd11c-5ea5-406e-6ec1-a544d048a9d6"),
                    CampaignContent = GetCampaignContents().First(),
                    ApplicationUser = User1()
                },
                  new SlideShowSession
                {
                    ApplicationUserId = userId,
                    BegTime = begDate.AddMinutes(2),
                    EndTime = begDate.AddMinutes(3),
                    CampaignContentId = GetCampaignContents().First().CampaignContentId,
                    IsPoll = false,
                    SlideShowSessionId = Guid.Parse("5d5cd11c-5ea5-406e-5ec1-a544d048a9d3"),
                    CampaignContent = GetCampaignContents().Skip(1).First(),
                    ApplicationUser = User1()
                },
                    new SlideShowSession
                {
                    ApplicationUserId = userId,
                    BegTime = begDate.AddMinutes(2),
                    EndTime = begDate.AddMinutes(3),
                    CampaignContentId = GetCampaignContents().First().CampaignContentId,
                    IsPoll = true,
                    SlideShowSessionId = Guid.NewGuid(),
                    CampaignContent = GetCampaignContents().Skip(1).First(),
                    ApplicationUser = User1()
                },
                      new SlideShowSession
                {
                    ApplicationUserId = User1().Id,
                    BegTime = begDate.AddDays(5),
                    EndTime = begDate.AddDays(5),
                    CampaignContentId = Guid.NewGuid(),
                    IsPoll = false,
                    SlideShowSessionId = Guid.NewGuid(),
                    CampaignContent = GetCampaignContents().Skip(1).First(),
                    ApplicationUser = User1()
                },
                 new SlideShowSession
                {
                    ApplicationUserId = User2().Id,
                    BegTime = begDate.AddDays(5),
                    EndTime = begDate.AddDays(5),
                    CampaignContentId = Guid.NewGuid(),
                    IsPoll = true,
                    SlideShowSessionId = Guid.NewGuid(),
                    CampaignContent = GetCampaignContents().Skip(1).First(),
                    ApplicationUser = User2()
                },
                  new SlideShowSession
                {
                    ApplicationUserId = User3().Id,
                    BegTime = begDate.AddMinutes(3),
                    EndTime = begDate.AddMinutes(5),
                    CampaignContentId = Guid.NewGuid(),
                    IsPoll = false,
                    SlideShowSessionId = Guid.NewGuid(),
                    CampaignContent = GetCampaignContents().Skip(1).First(),
                    ApplicationUser = User3()
                }
            }.AsQueryable();
        }
        //internal static List<SlideShowInfo> GetSlideShowInfos()
        //{
        //    return new List<SlideShowInfo>
        //    {
        //        new SlideShowInfo
        //        {
        //            ApplicationUserId = userId,
        //            BegTime = begDate,
        //            EndTime = begDate.AddMinutes(2),
        //            CampaignContentId = Guid.Parse("3d3cd11c-2ea0-406e-8ec1-a544d048a9d3"),
        //            ContentId = Guid.Parse("2d2cd22c-2ea2-406e-8ec1-a544d048a9d0"),
        //            DialogueId = null,// Guid.Parse("2d2cd22c-2ea0-406e-8ec1-a544d012a2d2"),
        //            Age = 30,
        //            IsPoll = false,
        //            ContentName = "test content",
        //            EmotionAttention = new UserOperations.Models.AnalyticModels.EmotionAttention()
        //            {
        //                //Attention = 0.5,
        //                //Negative = 0.3,
        //                //Neutral = 0.5,
        //                //Positive = 0.2
        //            },
        //             ContentType = "media",
        //             Gender = "1",
        //             Campaign = GetCampaigns().FirstOrDefault()
        //        },
        //          new SlideShowInfo
        //        {
        //            ApplicationUserId = userId,
        //            BegTime = begDate.AddMinutes(2),
        //            EndTime = begDate.AddMinutes(4),
        //            CampaignContentId = Guid.Parse("3d3cd11c-2ea0-406e-8ec1-a544d048a9d3"),
        //            ContentId = Guid.Parse("2d2cd22c-2ea2-406e-8ec1-a544d048a9d0"),
        //            DialogueId = null,// Guid.Parse("2d2cd22c-2ea0-406e-8ec1-a544d012a2d2"),
        //            Age = 30,
        //            IsPoll = false,
        //            ContentName = "test content",
        //            EmotionAttention = new UserOperations.Models.AnalyticModels.EmotionAttention()
        //            {
        //                //Attention = 0.7,
        //                //Negative = 0.3,
        //                //Neutral = 0.2,
        //                //Positive = 0.5
        //            },
        //             ContentType = "media",
        //             Gender = "1",
        //             Campaign = GetCampaigns().FirstOrDefault()
        //        }
        //    }.ToList();
        //}
        internal static List<SlideShowInfo> GetSlideShowInfosSimple()
        {
            return new List<SlideShowInfo>()
                {
                    new SlideShowInfo()
                    {
                        ContentName = "Content1",
                        IsPoll = false,
                        ContentType = "content",
                        ContentId = Guid.Parse("7ad7ceaf-cca5-4b6a-9d0e-59b7407ac2e1"),
                        Url = "https://test1.com"
                    },
                    new SlideShowInfo()
                    {
                        ContentName = "Content2",
                        IsPoll = false,
                        ContentType = "media",
                        ContentId = Guid.Parse("7ad7ceaf-cca5-4b6a-9d0e-59b7407ac2e2"),
                        Url = "https://test2.com"
                    },
                    new SlideShowInfo()
                    {
                        ContentName = "Content3",
                        IsPoll = true,
                        ContentType = "url",
                        ContentId = Guid.Parse("7ad7ceaf-cca5-4b6a-9d0e-59b7407ac2e3"),
                        Url = "https://test3.com"
                    }
                };
        }

        internal static IQueryable<CampaignContent> GetCampaignContents()
        {
            return new List<CampaignContent>
            {
                new CampaignContent
                {
                    CampaignContentId = Guid.Parse("3d3cd11c-2ea0-406e-8ec1-a544d048a9d3"),
                    Campaign = GetCampaigns().ToArray()[0],
                    CampaignId =  GetCampaigns().ToArray()[0].CampaignId,
                    Content = GetContents().ToArray()[0],
                    ContentId = GetContents().ToArray()[0].ContentId,
                    StatusId = 3
                },
                  new CampaignContent
                {
                    CampaignContentId = Guid.Parse("4d3cd11c-2ea0-406e-8ec1-a544d048a9d4"),
                    Campaign = GetCampaigns().ToArray()[0],
                    CampaignId =  GetCampaigns().ToArray()[0].CampaignId,
                    Content = GetContents().ToArray()[1],
                    ContentId = GetContents().ToArray()[1].ContentId,
                    StatusId = 5
                }
            }.AsQueryable();
        }

        internal static IQueryable<Campaign> GetCampaigns()
        {
            return new List<Campaign>
            {
                new Campaign
                {
                     BegAge = 5,
                     BegDate = new DateTime(),
                     CampaignId = Guid.Parse("1d1cd11c-2ea0-406e-8ec1-a544d048a9d0"),
                     CompanyId = GetCompanyIds().First(),
                     EndAge = 55,
                     EndDate = new DateTime(),
                     IsSplash = true,
                     Name = "test campaign",
                     StatusId = 3
                },
                  new Campaign
                {
                     BegAge = 5,
                     BegDate = new DateTime(),
                     CampaignId = Guid.Parse("1d1cd12c-2ea0-406e-8ec1-a544d048a9d0"),
                     CompanyId = GetCompanyIds().First(),
                     EndAge = 55,
                     EndDate = new DateTime(),
                     IsSplash = true,
                     Name = "test campaign 2",
                     StatusId = 3
                }
            }.AsQueryable();
        }

        internal static IQueryable<Content> GetContents()
        {
            return new List<Content>
            {
                new Content
                {
                    Duration = 5,
                    CompanyId = GetCompanyIds().First(),
                    ContentId = Guid.Parse("2d2cd22c-2ea2-406e-8ec1-a544d048a9d0"),
                    StatusId = 3,
                    Name = "test content",
                    IsTemplate = false,
                    JSONData = "{ggg:jjj}",
                    RawHTML = "kkk/ggg"
                },
                  new Content
                {
                    Duration = 5,
                    CompanyId = GetCompanyIds().First(),
                    ContentId = Guid.Parse("2d2cd23c-2ea2-406e-8ec1-a544d048a9d0"),
                    StatusId = 3,
                    Name = "test content",
                    IsTemplate = false,
                    JSONData = "{ggg:jjj}",
                    RawHTML = "kkk/ggg"
                }
            }.AsQueryable();
        }

        /// <summary>
        /// OTHER
        /// </summary>
        /// <returns></returns>
        internal static List<Guid> GetCompanyIdsAll()
        {
            return new List<Guid>
            {
                Guid.Parse("82560395-2cc3-46e8-bcef-c844f1048182"),
                Guid.Parse("77777395-2cc3-46e8-bcef-c844f1048182")
            };
        }
        internal static List<Guid> GetCompanyIds()
        {
            return new List<Guid>
            {
                Guid.Parse("82560395-2cc3-46e8-bcef-c844f1048182")
            };
        }

        internal static List<Company> GetCompanies()
        {
            return new List<Company>
            {
                Company1(),
                Company2()
        };
        }
        internal static Company Company1()
        {
            return new Company
            {
                CompanyId = Guid.Parse("82560395-2cc3-46e8-bcef-c844f1048182"),
                CompanyIndustryId = Guid.Parse("44440395-2cc3-46e8-bcef-c844f1048182"),
                CompanyName = "TEST Co",
                CorporationId = Guid.Parse("71aa39f1-649d-48d6-b1ae-10c518ed5979"),
                CountryId = Guid.Parse("66660395-2cc3-46e8-bcef-c844f1048182"),
                LanguageId = 2,
                StatusId = 3,
            };
        }
        internal static Company Company2()
        {
            return
                new Company
                {
                    CompanyId = Guid.Parse("77777395-2cc3-46e8-bcef-c844f1048182"),
                    CompanyIndustryId = Guid.Parse("44440395-2cc3-46e8-bcef-c844f1048182"),
                    CompanyName = "TEST Co2",
                    CorporationId = null,
                    CountryId = Guid.Parse("66660395-2cc3-46e8-bcef-c844f1048182"),
                    LanguageId = 2,
                    StatusId = 3,
                };
        }
        internal static List<ApplicationUser> GetUsersCompany1()
        {
            return new List<ApplicationUser>
            {
              User1(),
              User2()
            };
        }
        internal static ApplicationUser User1()
        {
            return new ApplicationUser
            {
                CompanyId = Company1().CompanyId,
                Avatar = null,
                StatusId = 3,
                Email = "test1@heedbook.com",
                FullName = "USER TEST NAME",
                Id = Guid.Parse("8d5cd62c-2ea0-406e-8ec1-a544d048a9d0"),
                NormalizedEmail = "test1@heedbook.com".ToUpper(),
                UserName = "test1@heedbook.com",
                NormalizedUserName = "test1@heedbook.com".ToUpper(),
                EmpoyeeId = "123",
                PasswordHash = "hhh",
                Dialogue = null,//GetDialoguesIncluded().ToList(),
                UserRoles = new List<ApplicationUserRole>()
                {new ApplicationUserRole{RoleId = Guid.Parse("8f8947da-5f76-4c6e-2222-170290c87194")}},
                // new List<ApplicationUserRole> { UserRoleIncluded() },
                //Company = Company()
            };
        }
        internal static ApplicationUser User2()
        {
            return new ApplicationUser
            {
                CompanyId = Company1().CompanyId,
                Avatar = null,
                StatusId = 3,
                Email = "test2@heedbook.com",
                FullName = "USER TEST NAME 2",
                Id = Guid.Parse("1d5cd61c-1ea1-406e-8ec1-a544d048a9d0"),
                NormalizedEmail = "test2@heedbook.com".ToUpper(),
                UserName = "test2@heedbook.com",
                NormalizedUserName = "test2@heedbook.com".ToUpper(),
                EmpoyeeId = "123",
                PasswordHash = "hhh",
                Dialogue = null,//GetDialoguesIncluded().ToList(),
                UserRoles = null,// new List<ApplicationUserRole> { UserRoleIncluded() },
                Company = Company1()
            };
        }
        internal static ApplicationUser User3()
        {
            return new ApplicationUser
            {
                CompanyId = Company2().CompanyId,
                Avatar = null,
                StatusId = 3,
                Email = "test3@heedbook.com",
                FullName = "USER TEST NAME 3",
                Id = Guid.Parse("2d5cd22c-2ea2-226e-8ec1-a544d048a9d0"),
                NormalizedEmail = "test3@heedbook.com".ToUpper(),
                UserName = "test3@heedbook.com",
                NormalizedUserName = "test3@heedbook.com".ToUpper(),
                EmpoyeeId = "123",
                PasswordHash = "hhh",
                Dialogue = null,//GetDialoguesIncluded().ToList(),
                UserRoles = null,// new List<ApplicationUserRole> { UserRoleIncluded() },
                Company = Company2()
            };
        }
        internal static ApplicationRole EmployeeRoleIncluded()
        {
            return new ApplicationRole
            {
                Id = Guid.Parse("2d5cd62c-2ea0-406e-8ec1-a222d048a9d0"),
                Name = "Employee",
                UserRoles = new List<ApplicationUserRole> { UserRoleIncluded() }
            };
        }
        internal static List<ApplicationRole> GetApplicationRoles()
        {
            var roles = new List<ApplicationRole>()
            {
                new ApplicationRole{Id = Guid.NewGuid(), Name = "Manager"},
                new ApplicationRole{Id = Guid.NewGuid(), Name = "User"},
                new ApplicationRole{Id = Guid.NewGuid(), Name = "Employee"},
                new ApplicationRole{Id = Guid.NewGuid(), Name = "Supervisor"},
                new ApplicationRole{Id = Guid.NewGuid(), Name = "Admin"},
                new ApplicationRole{Id = Guid.NewGuid(), Name = "Teacher"}
            };
            return roles;
        }

        internal static ApplicationUserRole UserRoleIncluded()
        {
            return new ApplicationUserRole
            {
                RoleId = EmployeeRoleIncluded().Id,
                UserId = User1().Id,
                Role = EmployeeRoleIncluded(),
                User = null
            };
        }
        internal static async Task<IEnumerable<BenchmarkModel>> GetBenchmarkList()
        {
            return new List<BenchmarkModel>{
                new BenchmarkModel
                {
                    Name = "SatisfactionIndexIndustryAvg",
                    Value = 0.7
                }};

        }
        internal static List<BenchmarkModel> GetBenchmarkModels(string benchmarkName)
        {
            var benchmarkModels = new List<BenchmarkModel>()
            {
                new BenchmarkModel{Name = benchmarkName, Value = 80},
                new BenchmarkModel{Name = benchmarkName, Value = 70},
                new BenchmarkModel{Name = benchmarkName, Value = 60},
            };
            return benchmarkModels;
        }
        internal static int GetStatus(string status)
        {
            var value = status == "Active" ? 3 : (status == "Inactive" ? 5 : 0);
            return value;
        }
        internal static Dictionary<string, string> GetClaims()
        {
            return new Dictionary<string, string>
            {
                {"sub", fullName} ,{"jti", "afd7fc64 - 802e-486b - a9b2 - 4ef824cb3b89"},
                {"applicationUserId", "8d5cd62c-2ea0-406e-8ec1-a544d048a9d0" },
                {"applicationUserName", "tuisv@heedbook.com"},
                {"companyName", companyName},
                {"companyId", "82560395-2cc3-46e8-bcef-c844f1048182"},
                {"corporationId", "71aa39f1-649d-48d6-b1ae-10c518ed5979"},
                {"languageCode", "2"},
                {"role", "Supervisor"},
                {"fullName", "tuisv@heedbook.com"},
                {"avatar",null},
                {"exp", "1575021022"},
                {"iss", "https://heedbook.com"},
                {"aud", "https://heedbook.com"}
         };
        }
        internal static IQueryable<ApplicationUser> GetUsers()
        {
            var users = new List<ApplicationUser>()
            {
                new ApplicationUser
                {
                    Id = new Guid("14f335c2-c64f-42cc-8ca3-dadd6a623ae1"),
                    Email = "IvanovIvan@heedbook.com",
                    CompanyId = new Guid("14f335c2-c64f-42cc-8ca3-dadd6a623ae2"),
                    Company = new Company
                    {
                        CompanyId = new Guid("14f335c2-c64f-42cc-8ca3-dadd6a623ae2")
                    },
                    UserRoles = new List<ApplicationUserRole>
                    {
                        new ApplicationUserRole{}
                    },
                    FullName = "Ivanov Ivan"
                }
            }.AsQueryable();
            return users;
        }
        internal static IQueryable<Company> GetCompanys()
        {
            var companys = new List<Company>
            {
                new Company
                {
                    CompanyId = new Guid("14f335c2-c64f-42cc-8ca3-dadd6a623ae2")
                }
            }.AsQueryable();
            return companys;
        }
        internal static IQueryable<Tariff> GetTariffs()
        {
            var tariffs = new List<Tariff>
            {
                new Tariff
                {
                    TariffId = new Guid("14f335c2-c64f-42cc-8ca3-dadd6a623ae2"),
                    CompanyId = new Guid("14f335c2-c64f-42cc-8ca3-dadd6a623ae2")
                }
            }.AsQueryable();
            return tariffs;
        }
        internal static IQueryable<Transaction> GetTransactions()
        {
            var transactions = new TestAsyncEnumerable<Transaction>(
                new List<Transaction>
                {
                    new Transaction
                    {
                        TariffId = new Guid("14f335c2-c64f-42cc-8ca3-dadd6a623ae2")
                    }
                }).AsQueryable();
            return transactions;
        }
       
        internal static IQueryable<PhraseCompany> GetPhraseCompanies()
        {
            var phraseCompanys = new TestAsyncEnumerable<PhraseCompany>(
                new List<PhraseCompany>
                {
                    new PhraseCompany{CompanyId = new Guid("14f335c2-c64f-42cc-8ca3-dadd6a623ae2")},
                    new PhraseCompany{CompanyId = new Guid("14f335c2-c64f-42cc-8ca3-dadd6a623ae2")},
                    new PhraseCompany{CompanyId = new Guid("14f335c2-c64f-42cc-8ca3-dadd6a623ae2")},
                    new PhraseCompany{CompanyId = new Guid("14f335c2-c64f-42cc-8ca3-dadd6a623ae2")}
                }).AsQueryable();
            return phraseCompanys;
        }
    
        internal static List<Guid> GetGuids()
        {
            return new List<Guid>() { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        }
        internal static IQueryable<T> GetEmptyList<T>()
        {
            return new List<T>().AsQueryable();
        }
        internal static List<Benchmark> GetBenchmarks()
        {
            var benchmarks = new List<Benchmark>
            {
                new Benchmark
                {
                    Day = new DateTime(2019, 11, 11, 12, 00, 00),
                    Id = new Guid("15b74216-7871-4f5b-b21f-9bcf5177a120"),
                    BenchmarkNameId = new Guid("15b74216-7871-4f5b-b21f-9bcf5177a120"),
                    IndustryId = new Guid("15b74216-7871-4f5b-b21f-9bcf5177a12b")
                },
                new Benchmark
                {
                    Day = new DateTime(2019, 11, 11, 13, 00, 00),
                    Id = new Guid("25b74216-7871-4f5b-b21f-9bcf5177a120"),
                    BenchmarkNameId = new Guid("25b74216-7871-4f5b-b21f-9bcf5177a120"),
                    IndustryId = new Guid("15b74216-7871-4f5b-b21f-9bcf5177a12b")
                },
                new Benchmark
                {
                    Day = new DateTime(2019, 11, 11, 14, 00, 00),
                    Id = new Guid("35b74216-7871-4f5b-b21f-9bcf5177a120"),
                    BenchmarkNameId = new Guid("35b74216-7871-4f5b-b21f-9bcf5177a120"),
                    IndustryId = new Guid("15b74216-7871-4f5b-b21f-9bcf5177a12b")
                }
            };
            return benchmarks;
        }
        internal static List<BenchmarkName> GetBenchmarkName()
        {
            var benchmarkName = new List<BenchmarkName>
            {
                new BenchmarkName
                {
                    Id = new Guid("15b74216-7871-4f5b-b21f-9bcf5177a120")
                },
                new BenchmarkName
                {
                    Id = new Guid("25b74216-7871-4f5b-b21f-9bcf5177a120")
                },
                new BenchmarkName
                {
                    Id = new Guid("35b74216-7871-4f5b-b21f-9bcf5177a120")
                },
            };
            return benchmarkName;
        }
        internal static UserRegister GetUserRegister()
        {
            return new UserRegister()
            {
                CompanyIndustryId = Guid.NewGuid(),                
                CompanyName = "HornAndHoves",
                LanguageId = 2,
                CountryId = Guid.NewGuid(),
                CorporationId = Guid.NewGuid(),
                Email = "HornAndHoves@heedbook.com",
                FullName = "HornAndHoves",
                Password = "123456"
            };
        }
        internal static IQueryable<Session> GetQueryableSessions()
        {
            //Arrange
            var user1 = new ApplicationUser
            {
                Id = Guid.Parse("b54d371a-acbc-48a0-9731-ab1d9a1f171f"),
                FullName = "MakarovMakar",
                CompanyId = Guid.Parse("154d371a-acbc-48a0-9731-ab1d9a1f1710"),
                UserRoles = new List<ApplicationUserRole>{new ApplicationUserRole{RoleId = Guid.Parse("b54d371a-1111-48a0-9731-ab1d9a1f171a")}}
            };
            var user2 = new ApplicationUser
            {
                Id = Guid.Parse("b54d371a-acbc-48a0-9731-ab1d9a1f171a"),
                FullName = "MakarovIvan",
                CompanyId = Guid.Parse("154d371a-acbc-48a0-9731-ab1d9a1f1710"),
                UserRoles = new List<ApplicationUserRole>{new ApplicationUserRole{RoleId = Guid.Parse("b54d371a-2222-48a0-9731-ab1d9a1f171a")}}
            };
            var user3 = new ApplicationUser
            {
                Id = Guid.Parse("b54d371a-acbc-48a0-9731-ab1d9a1f171b"),
                FullName = "MakarovRoman",
                CompanyId = Guid.Parse("154d371a-acbc-48a0-9731-ab1d9a1f1710"),
                UserRoles = new List<ApplicationUserRole>{new ApplicationUserRole{RoleId = Guid.Parse("b54d371a-3333-48a0-9731-ab1d9a1f171a")}}
            
            };
            var sessions = new List<Session>
            {
                new Session
                {
                    BegTime = new DateTime(2019, 11, 26, 12, 00, 00),
                    EndTime = new DateTime(2019, 11, 26, 12, 30, 00),
                    StatusId = 7,
                    ApplicationUserId = Guid.Parse("b54d371a-acbc-48a0-9731-ab1d9a1f171f"),
                    ApplicationUser = user1
                },
                new Session
                {
                    BegTime = new DateTime(2019, 11, 26, 13, 00, 00),
                    EndTime = new DateTime(2019, 11, 26, 13, 30, 00),
                    StatusId = 7,
                    ApplicationUserId = Guid.Parse("b54d371a-acbc-48a0-9731-ab1d9a1f171a"),
                    ApplicationUser = user2
                },
                new Session
                {
                    BegTime = new DateTime(2019, 11, 26, 14, 00, 00),
                    EndTime = new DateTime(2019, 11, 26, 14, 30, 00),
                    StatusId = 6,
                    ApplicationUserId = Guid.Parse("b54d371a-acbc-48a0-9731-ab1d9a1f171b"),
                    ApplicationUser = user3
                },
            }.AsQueryable();
            return sessions;
        }
    }
    internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
        {
            return new TestAsyncEnumerable<TResult>(expression);
        }

        public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            return Task.FromResult(Execute<TResult>(expression));
        }
    }
    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable)
            : base(enumerable)
        { }

        public TestAsyncEnumerable(Expression expression)
            : base(expression)
        { }

        public IAsyncEnumerator<T> GetEnumerator()
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IQueryProvider IQueryable.Provider
        {
            get { return new TestAsyncQueryProvider<T>(this); }
        }
    }
    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public void Dispose()
        {
            _inner.Dispose();
        }

        public T Current
        {
            get
            {
                return _inner.Current;
            }
        }

        public Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            return Task.FromResult(_inner.MoveNext());
        }
    }
}