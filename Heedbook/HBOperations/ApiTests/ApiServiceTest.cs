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
using UserOperations.Providers;
using UserOperations.Providers.Interfaces;
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
        public Mock<ILoginService> MockILoginService(Mock<ILoginService> moqILoginService)
        {
            moqILoginService.Setup(p => p.CheckUserLogin(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);
            moqILoginService.Setup(p => p.SaveErrorLoginHistory(It.IsAny<Guid>(), It.IsAny<string>()))
                .Returns(true);
            var dict = new Dictionary<string, string>
            {
                {"role", "role"},
                {"companyId", "b1ec1819-5782-4215-86d0-b3ccbeaddaef"},
                {"applicationUserId", "2d10136f-8341-4758-b218-785409a11e98"}
            };
            moqILoginService.Setup(p => p.GetDataFromToken(It.IsAny<string>(), out dict, null))
                .Returns(true);
            moqILoginService.Setup(p => p.GeneratePasswordHash(It.IsAny<string>()))
                .Returns("Hash");
            moqILoginService.Setup(p => p.SavePasswordHistory(It.IsAny<Guid>(), It.IsAny<string>()))
                .Returns(true);
            moqILoginService.Setup(p => p.GeneratePass(6))
                .Returns("123456");
            moqILoginService.Setup(p => p.CreateTokenForUser(It.IsAny<ApplicationUser>(), It.IsAny<bool>()))
                .Returns("Token");
            return moqILoginService;
        }
        public Mock<IMailSender> MockIMailSender(Mock<IMailSender> moqIMailSender)
        {
            moqIMailSender.Setup(p => p.SendRegisterEmail(new HBData.Models.ApplicationUser()))
                .Returns(Task.FromResult(0));
            moqIMailSender.Setup(p => p.SendPasswordChangeEmail(new HBData.Models.ApplicationUser(), "password"))
                .Returns(Task.FromResult(0));
            return moqIMailSender;
        }
        public Mock<IAccountProvider> MockIAccountProvider(Mock<IAccountProvider> moqIAccountProvider)
        {            
            moqIAccountProvider.Setup(p => p.GetStatusId(It.IsAny<string>()))
                .Returns((string p) => p == "Active" ? 3 : (p == "Inactive" ? 5 : 0));
            moqIAccountProvider.Setup(p => p.CompanyExist(It.IsAny<string>()))
                .Returns(Task.FromResult(false));
            moqIAccountProvider.Setup(p => p.EmailExist(It.IsAny<string>()))
                .Returns(Task.FromResult(false));
            moqIAccountProvider.Setup(p => p.AddNewCompanysInBase(new UserRegister(), Guid.NewGuid()))
                .Returns(Task.FromResult(new Company()));
            moqIAccountProvider.Setup(p => p.AddNewUserInBase(new UserRegister(), Guid.NewGuid()))
                .Returns(Task.FromResult(new ApplicationUser()));
            moqIAccountProvider.Setup(p => p.AddUserRoleInBase(new UserRegister(), new ApplicationUser()))
                .Returns(Task.FromResult(0));
            moqIAccountProvider.Setup(p => p.GetTariffs(Guid.NewGuid()))
                .Returns(0);
            moqIAccountProvider.Setup(p => p.CreateCompanyTariffAndtransaction(new Company()))
                .Returns(Task.FromResult(0));
            moqIAccountProvider.Setup(p => p.AddWorkerType(new Company()))
                .Returns(Task.FromResult(0));
            moqIAccountProvider.Setup(p => p.AddContentAndCampaign(new Company()))
                .Returns(Task.FromResult(0));
            moqIAccountProvider.Setup(p => p.SaveChangesAsync())
                .Callback(() => {});
            moqIAccountProvider.Setup(p => p.SaveChanges())
                .Callback(() => {});
            var user = new ApplicationUser(){UserName = "TestUser", StatusId = 3, PasswordHash = ""};
            moqIAccountProvider.Setup(p => p.GetUserIncludeCompany(It.IsAny<string>()))
                .Returns(user);
            moqIAccountProvider.Setup(p => p.GetUserIncludeCompany(It.IsAny<Guid>(), It.IsAny<AccountAuthorization>()))
                .Returns(user);
            moqIAccountProvider.Setup(p => p.RemoveAccount("email"))
                .Callback(() => {});          
            return moqIAccountProvider;
        }
        public Mock<IHelpProvider> MockIHelpProvider(Mock<IHelpProvider> moqIHelpProvider)
        {
            moqIHelpProvider.Setup(p => p.AddComanyPhrases());
            moqIHelpProvider.Setup(p => p.CreatePoolAnswersSheet(It.IsAny<List<AnswerInfo>>(), It.IsAny<string>()))
                .Returns(new MemoryStream());
            return moqIHelpProvider;
        }
        private int GetStatus(string status)
        {
            var value = status == "Active" ? 3 : (status == "Inactive" ? 5 : 0);
            return value;
        }   
        public Mock<IAnalyticContentProvider> MockIAnalyticContentProvider(Mock<IAnalyticContentProvider> moqIAnalyticContentProvider)
        {
            moqIAnalyticContentProvider.Setup(p => p.GetSlideShowsForOneDialogueAsync(It.IsAny<Dialogue>()))
                .Returns(Task.FromResult(new List<SlideShowInfo>()
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
                }));
            moqIAnalyticContentProvider.Setup(p => p.GetSlideShowFilteredByPoolAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<List<Guid>>(),
                    It.IsAny<List<Guid>>(),
                    It.IsAny<List<Guid>>(),
                    It.IsAny<bool>()))
                .Returns(Task.FromResult(new List<SlideShowInfo>()
                {
                    new SlideShowInfo(){BegTime = new DateTime(), EndTime = new DateTime(), DialogueId = Guid.NewGuid(), Age = 20, Gender = "male"}, 
                    new SlideShowInfo(){BegTime = new DateTime(), EndTime = new DateTime(), DialogueId = Guid.NewGuid(), Age = 22, Gender = "female"}, 
                    new SlideShowInfo(){BegTime = new DateTime(), EndTime = new DateTime(), DialogueId = Guid.NewGuid(), Age = 27, Gender = "male"}
                }));
            moqIAnalyticContentProvider.Setup(p => p.GetAnswersInOneDialogueAsync(
                    It.IsAny<List<SlideShowInfo>>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<Guid>()))
                .Returns(Task.FromResult(new List<CampaignContentAnswer>(){}));
            moqIAnalyticContentProvider.Setup(p => p.GetAnswersForOneContent(
                    It.IsAny<List<AnswerInfo.AnswerOne>>(),
                    It.IsAny<Guid?>()))
                .Returns(new List<AnswerInfo.AnswerOne>(){});
            moqIAnalyticContentProvider.Setup(p => p.GetConversion(
                    It.IsAny<double>(),
                    It.IsAny<double>()))
                .Returns(0.5d);
            moqIAnalyticContentProvider.Setup(p => p.GetAnswersFullAsync(
                    It.IsAny<List<SlideShowInfo>>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<List<Guid>>(),
                    It.IsAny<List<Guid>>(),
                    It.IsAny<List<Guid>>()))
                .Returns(Task.FromResult(new List<AnswerInfo.AnswerOne>(){}));
            moqIAnalyticContentProvider.Setup(p => p.AddDialogueIdToShow(It.IsAny<List<SlideShowInfo>>(), It.IsAny<List<DialogueInfoWithFrames>>()))
                .Returns(new List<SlideShowInfo>(){});
            moqIAnalyticContentProvider.Setup(p => p.EmotionsDuringAdv(It.IsAny<List<SlideShowInfo>>(), It.IsAny<List<DialogueInfoWithFrames>>()))
                .Returns(new EmotionAttention(){Positive = 0.3d, Negative = 0.3d, Neutral = 0.3d, Attention = 0.3d});
            moqIAnalyticContentProvider.Setup(p => p.EmotionDuringAdvOneDialogue(It.IsAny<List<SlideShowInfo>>(), It.IsAny<List<DialogueFrame>>()))
                .Returns(new EmotionAttention(){Positive = 0.3d, Negative = 0.3d, Neutral = 0.3d, Attention = 0.3d});
            return moqIAnalyticContentProvider;
        }
        public Mock<IRequestFilters> MockIRequestFiltersProvider(Mock<IRequestFilters> moqIRequestFiltersProvider)
        {
            var list = new List<Guid>(){};
            moqIRequestFiltersProvider.Setup(p => p.CheckRolesAndChangeCompaniesInFilter( ref list, It.IsAny<List<Guid>>(), It.IsAny<string>(), It.IsAny<Guid>()));
            moqIRequestFiltersProvider.Setup(p => p.GetBegDate(It.IsAny<string>()))
                .Returns(new DateTime(2019, 10, 30));
            moqIRequestFiltersProvider.Setup(p => p.GetEndDate(It.IsAny<string>()))
                .Returns(new DateTime(2019, 11, 01));
            return moqIRequestFiltersProvider;
        }
        public Mock<IAnalyticOfficeProvider> MockIAnalyticOfficeProvider(Mock<IAnalyticOfficeProvider> moqIAnalyticOfficeProvider)
        {
            var sessionsInfo = new List<SessionInfo>
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
            moqIAnalyticOfficeProvider.Setup(p => p.GetSessionsInfo(
                It.IsAny<DateTime>(), 
                It.IsAny<DateTime>(), 
                It.IsAny<List<Guid>>(),
                It.IsAny<List<Guid>>(),
                It.IsAny<List<Guid>>()))
                .Returns(sessionsInfo);
            var dialogues = new List<DialogueInfo>()
            {
                new DialogueInfo(){BegTime = new DateTime(2019, 10, 29, 18, 30, 00), EndTime = new DateTime(2019, 10, 29, 19, 00, 00)},
                new DialogueInfo(){BegTime = new DateTime(2019, 10, 30, 18, 30, 00), EndTime = new DateTime(2019, 10, 30, 19, 00, 00)},
                new DialogueInfo(){BegTime = new DateTime(2019, 10, 30, 19, 10, 00), EndTime = new DateTime(2019, 10, 30, 19, 40, 00)},
                new DialogueInfo(){BegTime = new DateTime(2019, 10, 30, 19, 50, 00), EndTime = new DateTime(2019, 10, 30, 20, 20, 00)},
                new DialogueInfo(){BegTime = new DateTime(2019, 10, 30, 19, 10, 00), EndTime = new DateTime(2019, 10, 30, 19, 40, 00)},
                new DialogueInfo(){BegTime = new DateTime(2019, 10, 30, 20, 30, 00), EndTime = new DateTime(2019, 10, 30, 21, 00, 00)},
                new DialogueInfo(){BegTime = new DateTime(2019, 10, 30, 21, 10, 00), EndTime = new DateTime(2019, 10, 30, 21, 40, 00)},
                new DialogueInfo(){BegTime = new DateTime(2019, 10, 31, 18, 30, 00), EndTime = new DateTime(2019, 10, 31, 19, 00, 00)},
                new DialogueInfo(){BegTime = new DateTime(2019, 11, 01, 18, 30, 00), EndTime = new DateTime(2019, 11, 01, 19, 00, 00)}
            };
            moqIAnalyticOfficeProvider.Setup(p => p.GetDialoguesInfo(
                It.IsAny<DateTime>(), 
                It.IsAny<DateTime>(), 
                It.IsAny<List<Guid>>(),
                It.IsAny<List<Guid>>(),
                It.IsAny<List<Guid>>()))
                .Returns(dialogues);
            return moqIAnalyticOfficeProvider;
        }
        public Mock<IDBOperations> MockIDBOperations(Mock<IDBOperations> moqIDBOperationsProvider)
        {
            moqIDBOperationsProvider.Setup(p => p.LoadIndex(
                    It.IsAny<List<SessionInfo>>(),
                    It.IsAny<List<DialogueInfo>>(), 
                    It.IsAny<DateTime>(), 
                    It.IsAny<DateTime>()))
                .Returns(0.5d);
            moqIDBOperationsProvider.Setup(p => p.DialoguesCount(
                    It.IsAny<List<DialogueInfo>>(),
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>()))
                .Returns(3);
            moqIDBOperationsProvider.Setup(p => p.SessionAverageHours(
                    It.IsAny<List<SessionInfo>>(),                    
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>()))
                .Returns(5d);
            moqIDBOperationsProvider.Setup(p => p.DialogueAverageDuration(
                    It.IsAny<List<DialogueInfo>>(),                    
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>()))
                .Returns(5d);
            moqIDBOperationsProvider.Setup(p => p.BestEmployeeLoad(
                    It.IsAny<List<DialogueInfo>>(),
                    It.IsAny<List<SessionInfo>>(), 
                    It.IsAny<DateTime>(), 
                    It.IsAny<DateTime>()))
                .Returns(new Employee());
            moqIDBOperationsProvider.Setup(p => p.SatisfactionIndex(
                    It.IsAny<List<DialogueInfo>>()))
                .Returns(60d);
            moqIDBOperationsProvider.Setup(p => p.EmployeeCount(
                    It.IsAny<List<DialogueInfo>>()))
                .Returns(3);
            moqIDBOperationsProvider.Setup(p => p.DialogueAveragePause(
                    It.IsAny<List<SessionInfo>>(),
                    It.IsAny<List<DialogueInfo>>(), 
                    It.IsAny<DateTime>(), 
                    It.IsAny<DateTime>()))
                .Returns(20d);
            moqIDBOperationsProvider.Setup(p => p.DialogueAvgPauseListInMinutes(
                    It.IsAny<List<SessionInfo>>(),
                    It.IsAny<List<DialogueInfo>>(), 
                    It.IsAny<DateTime>(), 
                    It.IsAny<DateTime>()))
                .Returns(new List<double>(){});
            moqIDBOperationsProvider.Setup(p => p.SessionTotalHours(
                    It.IsAny<List<SessionInfo>>(),
                    It.IsAny<DateTime>(), 
                    It.IsAny<DateTime>()))
                .Returns(9d);
            
            return moqIDBOperationsProvider;
        }
        public void Dispose()
        {
        }
    }
}