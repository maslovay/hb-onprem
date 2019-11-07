using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using Microsoft.Extensions.Configuration;
using Moq;
using UserOperations.Models.AnalyticModels;
using UserOperations.Providers;
using UserOperations.Services;
using UserOperations.Utils;

namespace ApiTests
{
    public abstract class ApiServiceTest : IDisposable
    {
        protected Mock<IGenericRepository> repositoryMock;

        protected Mock<IRequestFilters> filterMock;
        protected Mock<IConfiguration> configMock;
        protected Mock<ILoginService> loginMock;
        protected Mock<IDBOperations> dbOperationMock;
        protected Mock<IAnalyticHomeProvider> homeProviderMock;
        protected Mock<IAnalyticCommonProvider> commonProviderMock;
        protected Mock<IMailSender> mailSenderMock;

        protected string beg, end;
        protected DateTime begDate, endDate, prevDate;
        protected string token;
        protected Dictionary<string, string> tokenclaims;
        protected List<Guid> companyIds;

        protected virtual void InitData()
        {

        }

        protected virtual void InitServices() { }
        public void Setup()
        {
            repositoryMock = new Mock<IGenericRepository>();

            filterMock = new Mock<IRequestFilters>(MockBehavior.Loose);
            configMock = new Mock<IConfiguration>();
            loginMock = new Mock<ILoginService>(MockBehavior.Loose);
            dbOperationMock = new Mock<IDBOperations>();
            homeProviderMock = new Mock<IAnalyticHomeProvider>();
            commonProviderMock = new Mock<IAnalyticCommonProvider>();
            mailSenderMock = new Mock<IMailSender>();

            InitData();
            InitServices();
        }


        /// <summary>
        ///     GET DATA
        /// </summary>
        /// <returns></returns>

        protected Dictionary<string, string> GetClaims()
        {
            return new Dictionary<string, string>
            {
                {"sub", "tuisv@heedbook.com"} ,{"jti", "afd7fc64 - 802e-486b - a9b2 - 4ef824cb3b89"},
                {"applicationUserId", "8d5cd62c-2ea0-406e-8ec1-a544d048a9d0" },
                {"applicationUserName", "tuisv@heedbook.com"},
                {"companyName", "TUI Supervisor"},
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

        protected List<Guid> GetCompanyIds()
        {
            return new List<Guid>
            {
                Guid.Parse("82560395-2cc3-46e8-bcef-c844f1048182")//,
              //  Guid.Parse("ddaa6e0b-3439-4c78-915e-2e0245332db3"),
              //  Guid.Parse("ddaa7e0b-3439-4c78-915e-2e0245332db4")
            };
        }

        protected async Task<IEnumerable<SessionInfo>> GetSessions()
        {
            return new List<SessionInfo>
            {
                new SessionInfo
                {
                    ApplicationUserId = Guid.Parse("8d5cd62c-2ea0-406e-8ec1-a544d048a9d0"),
                    BegTime = new DateTime(2019,10,04, 12, 19,00),
                    EndTime = new DateTime(2019,10,04,12,20,25),
                    CompanyId = Guid.Parse("82560395-2cc3-46e8-bcef-c844f1048182"),
                    FullName = "tuisv@heedbook.com",
                    IndustryId = Guid.Parse("99960395-2cc3-46e8-bcef-c844f1048999")
                },
                  new SessionInfo
                {
                    ApplicationUserId = Guid.Parse("8d5cd62c-2ea0-406e-8ec1-a544d048a9d0"),
                    BegTime = new DateTime(2019,10,04, 18, 19,00),
                    EndTime = new DateTime(2019,10,04,18,25,30),
                    CompanyId = Guid.Parse("82560395-2cc3-46e8-bcef-c844f1048182"),
                    FullName = "tuisv@heedbook.com",
                    IndustryId = Guid.Parse("99960395-2cc3-46e8-bcef-c844f1048999")
                }
            };
        }

        protected IQueryable<SlideShowSession> GetSlideShowSessions()
        {
            return new List<SlideShowSession>
            {
                new SlideShowSession
                {
                    ApplicationUserId = Guid.Parse("8d5cd62c-2ea0-406e-8ec1-a544d048a9d0"),
                    BegTime = new DateTime(2019,10,04, 12, 19,00),
                    EndTime = new DateTime(2019,10,04,12,19,05),
                    CampaignContentId = Guid.Parse("3d3cd11c-2ea0-406e-8ec1-a544d048a9d3"),
                    IsPoll = false,
                    SlideShowSessionId = Guid.Parse("6d5cd11c-5ea5-406e-6ec1-a544d048a9d6")
                },
                  new SlideShowSession
                {
                    ApplicationUserId = Guid.Parse("8d5cd62c-2ea0-406e-8ec1-a544d048a9d0"),
                    BegTime = new DateTime(2019,10,04, 18, 19,00),
                    EndTime = new DateTime(2019,10,04,18,25,30),
                    CampaignContentId = Guid.Parse("3d3cd11c-2ea0-406e-8ec1-a544d048a9d3"),
                    IsPoll = false,
                    SlideShowSessionId = Guid.Parse("5d5cd11c-5ea5-406e-5ec1-a544d048a9d3")
                }
            }.AsQueryable();
        }


        protected List<SlideShowInfo> GetSlideShowInfos()
        {
            return new List<SlideShowInfo>
            {
                new SlideShowInfo
                {
                    ApplicationUserId = Guid.Parse("8d5cd62c-2ea0-406e-8ec1-a544d048a9d0"),
                    BegTime = new DateTime(2019,10,04, 12, 19,00),
                    EndTime = new DateTime(2019,10,04,12,19,05),
                    CampaignContentId = Guid.Parse("3d3cd11c-2ea0-406e-8ec1-a544d048a9d3"),
                    ContentId = Guid.Parse("2d2cd22c-2ea2-406e-8ec1-a544d048a9d0"),
                    DialogueId = Guid.Parse("2d2cd22c-2ea0-406e-8ec1-a544d012a2d2"),
                    Age = 30,
                    IsPoll = false,
                    ContentName = "test content",
                    EmotionAttention = new EmotionAttention()
                    {
                        Attention = 0.5,
                        Negative = 0.3,
                        Neutral = 0.5,
                        Positive = 0.2
                    },
                     ContentType = "media",
                     Gender = "1",
                     Campaign = GetCampaigns().FirstOrDefault()
                },
                  new SlideShowInfo
                {
                    ApplicationUserId = Guid.Parse("8d5cd62c-2ea0-406e-8ec1-a544d048a9d0"),
                    BegTime = new DateTime(2019,10,04, 12, 19,05),
                    EndTime = new DateTime(2019,10,04,12,19,10),
                    CampaignContentId = Guid.Parse("3d3cd11c-2ea0-406e-8ec1-a544d048a9d3"),
                    ContentId = Guid.Parse("2d2cd22c-2ea2-406e-8ec1-a544d048a9d0"),
                    DialogueId = Guid.Parse("2d2cd22c-2ea0-406e-8ec1-a544d012a2d2"),
                    Age = 30,
                    IsPoll = false,
                    ContentName = "test content",
                    EmotionAttention = new EmotionAttention()
                    {
                        Attention = 0.7,
                        Negative = 0.3,
                        Neutral = 0.2,
                        Positive = 0.5
                    },
                     ContentType = "media",
                     Gender = "1",
                     Campaign = GetCampaigns().FirstOrDefault()
                }
            }.ToList();
        }

        protected IQueryable<CampaignContent> GetCampaignContents()
        {
            return new List<CampaignContent>
            {
                new CampaignContent
                {
                    CampaignContentId = Guid.Parse("3d3cd11c-2ea0-406e-8ec1-a544d048a9d3"),
                    CampaignId = Guid.Parse("1d1cd11c-2ea0-406e-8ec1-a544d048a9d0"),
                    ContentId = Guid.Parse("2d2cd22c-2ea2-406e-8ec1-a544d048a9d0"),
                    StatusId = 3
                },
                  new CampaignContent
                {
                    CampaignContentId = Guid.Parse("4d3cd11c-2ea0-406e-8ec1-a544d048a9d4"),
                    CampaignId = Guid.Parse("1d1cd12c-2ea0-406e-8ec1-a544d048a9d0"),
                    ContentId = Guid.Parse("2d2cd23c-2ea2-406e-8ec1-a544d048a9d0"),
                    StatusId = 5
                }
            }.AsQueryable();
        }

        protected IQueryable<Campaign> GetCampaigns()
        {
            return new List<Campaign>
            {
                new Campaign
                {
                     BegAge = 5,
                     BegDate = new DateTime(),
                     CampaignId = Guid.Parse("1d1cd11c-2ea0-406e-8ec1-a544d048a9d0"),
                     CompanyId = Guid.Parse("82560395-2cc3-46e8-bcef-c844f1048182"),
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
                     CompanyId = Guid.Parse("82560395-2cc3-46e8-bcef-c844f1048182"),
                     EndAge = 55,
                     EndDate = new DateTime(),
                     IsSplash = true,
                     Name = "test campaign 2",
                     StatusId = 3
                }
            }.AsQueryable();
        }

        protected IQueryable<Content> GetContents()
        {
            return new List<Content>
            {
                new Content
                {
                    Duration = 5,
                    CompanyId = Guid.Parse("82560395-2cc3-46e8-bcef-c844f1048182"),
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
                    CompanyId = Guid.Parse("82560395-2cc3-46e8-bcef-c844f1048182"),
                    ContentId = Guid.Parse("2d2cd23c-2ea2-406e-8ec1-a544d048a9d0"),
                    StatusId = 3,
                    Name = "test content",
                    IsTemplate = false,
                    JSONData = "{ggg:jjj}",
                    RawHTML = "kkk/ggg"
                }
            }.AsQueryable();
        }

        protected async Task<IEnumerable<SessionInfo>> GetEmptySessions()
        {
            return null;
        }

        protected async Task<Guid> GetCrossPhraseId()
        {
            return Guid.Parse("55560395-2cc3-46e8-bcef-c844f1048555");
        }

        protected IQueryable<Dialogue> GetDialoguesWithUserPhrasesSatisfaction()
        {
            var dialogues = new List<Dialogue>
            {
                new Dialogue
                {
                    ApplicationUserId = Guid.Parse("8d5cd62c-2ea0-406e-8ec1-a544d048a9d0"),
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
                        new DialoguePhrase
                        {
                            DialogueId = Guid.Parse("2d2cd22c-2ea0-406e-8ec1-a544d012a2d2"),
                            DialoguePhraseId = Guid.Parse("5d5cd55c-5ea0-406e-8ec1-a544d012a2d2"),
                            IsClient = true,
                            PhraseId = Guid.Parse("6d6cd66c-6ea0-606e-8ec1-a544d012a2d2"),
                            PhraseTypeId =  Guid.Parse("55560395-2cc3-46e8-bcef-c844f1048555")//cross
        }
                    },
                   ApplicationUser = new ApplicationUser
                   {
                       FullName = "tuisv@heedbook.com"
                   },
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
                    ApplicationUserId = Guid.Parse("8d5cd62c-2ea0-406e-8ec1-a544d048a9d0"),
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
                        new DialoguePhrase
                        {
                            DialogueId = Guid.Parse("4d4cd44c-2ea0-406e-8ec1-a544d012a3d3"),
                            DialoguePhraseId = Guid.Parse("7d5cd55c-5ea0-406e-8ec1-a544d012a2d7"),
                            IsClient = true,
                            PhraseId = Guid.Parse("6d6cd66c-6ea0-606e-8ec1-a544d012a2d2"),
                            PhraseTypeId = Guid.Parse("7d7cd77c-7ea0-406e-7ec1-a544d012a2d2")
                        }
                    },
                   ApplicationUser = new ApplicationUser
                   {
                       FullName = "tuisv@heedbook.com"
                   },
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

        protected IQueryable<Dialogue> GetDialoguesWithFrames()
        {
            var dialogues = new List<Dialogue>
            {
                new Dialogue
                {
                    ApplicationUserId = Guid.Parse("8d5cd62c-2ea0-406e-8ec1-a544d048a9d0"),
                    BegTime = new DateTime(2019,10,04, 12, 19,00),
                    EndTime = new DateTime(2019,10,04,12,20,25),
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
                            Time = new DateTime(2019,10,04, 12, 19,03),
                            AngerShare =  0.5
        }
                    },
                   ApplicationUser = new ApplicationUser
                   {
                       FullName = "tuisv@heedbook.com"
                   },
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
                    ApplicationUserId = Guid.Parse("8d5cd62c-2ea0-406e-8ec1-a544d048a9d0"),
                    BegTime = new DateTime(2019,10,04, 18, 19,00),
                    EndTime = new DateTime(2019,10,04,18,25,30),
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
                            Time = new DateTime(2019,10,04, 18, 19,03),
                            AngerShare =  0.2
        }
                    }                 
                }
            };
            return dialogues.AsQueryable();
        }

        protected IQueryable<Dialogue> GetEmptyDialogues()
        {
            return new List<Dialogue>().AsQueryable();
        }

        protected async Task<IEnumerable<BenchmarkModel>> GetBenchmarkList()
        {
            return new List<BenchmarkModel>{
                new BenchmarkModel
                {
                    Name = "SatisfactionIndexIndustryAvg",
                    Value = 0.7
                }};

        }



        public void Dispose()
        {
        }
    }
}