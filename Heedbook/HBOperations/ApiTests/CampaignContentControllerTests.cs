using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using System;
using HBData.Models;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using UserOperations.Models.AnalyticModels;
using UserOperations.Services;
using UserOperations.Models.Get.AnalyticServiceQualityController;
using UserOperations.Models.Get.AnalyticSpeechController;
using UserOperations.Models;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http.Internal;

namespace ApiTests
{
    public class CampaignContentControllerTests : ApiServiceTest
    {
        private CampaignContentService campaignContentService;
        [SetUp]
        public new void Setup()
        {
            base.Setup();
            campaignContentService = new CampaignContentService(
                moqILoginService.Object,
                requestFiltersMock.Object,
                repositoryMock.Object,
                sftpClient.Object,
                fileRefUtils.Object);
        }
        [Test]
        public async Task CampaignGetGetTest()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();
            var campaignContentId = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            var isActual = true;
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Supervisor");
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            var companyIds = new List<Guid>{companyId};
            requestFiltersMock.Setup(p => p.CheckRolesAndChangeCompaniesInFilter(ref companyIds, It.IsAny<List<Guid>>(), It.IsAny<string>(), It.IsAny<Guid>()));
            repositoryMock.Setup(p => p.GetAsQueryable<Status>())
                .Returns(new TestAsyncEnumerable<Status>(new List<Status>
                {
                    new Status
                    {
                        StatusName = "Active",
                        StatusId = 3
                    }
                }.AsQueryable()));
            repositoryMock.Setup(p => p.GetAsQueryable<Campaign>())
                .Returns(new TestAsyncEnumerable<Campaign>(new List<Campaign>
                {
                    new Campaign
                    {
                        CampaignId = campaignId,
                        CompanyId = companyId,
                        StatusId = 3,
                        BegDate = DateTime.Now.AddDays(-2),
                        EndDate = DateTime.Now.AddDays(2),
                        CampaignContents = new List<CampaignContent>
                        {
                            new CampaignContent
                            {
                                CampaignContentId = campaignContentId,
                            }
                        }
                    }
                }.AsQueryable()));
            
            //Act
            var result = campaignContentService.CampaignGet(
                new List<Guid>{companyId},
                new List<Guid>{corporationId},
                isActual);
            System.Console.WriteLine($"result:\n{JsonConvert.SerializeObject(result)}");

            //Assert
            Assert.IsFalse(result is null);
            Assert.IsTrue(result.Count > 0);
        }
        [Test]
        public async Task CampaignPostTest()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();
            var campaignContentId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            repositoryMock.Setup(p => p.GetAsQueryable<Status>())
                .Returns(new TestAsyncEnumerable<Status>(new List<Status>
                {
                    new Status
                    {
                        StatusName = "Active",
                        StatusId = 3
                    }
                }.AsQueryable()));
            var model = new CampaignPutPostModel(new Campaign
                {
                    CampaignId = campaignId,
                    CompanyId = companyId,
                    CreationDate = DateTime.Now.AddMinutes(-3),
                    StatusId = 3,
                    CampaignContents = new List<CampaignContent>{}
                }, 
                new List<CampaignContent>
                {
                    new CampaignContent
                    {
                        CampaignContentId = campaignContentId
                    }
                });

            //Act
            var campaign = campaignContentService.CampaignPost(model);
            System.Console.WriteLine($"result:\n{JsonConvert.SerializeObject(campaign)}");

            //Assert
            Assert.IsFalse(campaign is null);
            Assert.IsTrue(campaign.CompanyId == companyId);
        }
        [Test]
        public async Task CampaignPutPutTest()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();
            var campaignContentId = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            repositoryMock.Setup(p => p.GetAsQueryable<Status>())
                .Returns(new TestAsyncEnumerable<Status>(new List<Status>
                {
                    new Status
                    {
                        StatusName = "Active",
                        StatusId = 3
                    },
                    new Status
                    {
                        StatusName = "Inactive",
                        StatusId = 8
                    }
                }.AsQueryable()));
            var campaignContent = new CampaignContent
            {
                CampaignContentId = campaignContentId
            };
            var campaign = new Campaign
            {
                CampaignId = campaignId,
                CompanyId = companyId,
                CreationDate = DateTime.Now.AddMinutes(-3),
                StatusId = 3,
                CampaignContents = new List<CampaignContent>
                {
                    campaignContent
                }
            };            
            var model = new CampaignPutPostModel
            (
                campaign,
                new List<CampaignContent>
                {
                    campaignContent
                }
            );
            requestFiltersMock.Setup(p => p.IsCompanyBelongToUser(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<String>()))
                .Returns(true);
            repositoryMock.Setup(p => p.GetAsQueryable<Campaign>())
                .Returns(new TestAsyncEnumerable<Campaign>(new List<Campaign>
                {
                    campaign
                }.AsQueryable()));

            //Act
            var newCompanyId = Guid.NewGuid();
            model.Campaign.CompanyId = newCompanyId;
            var campaignResult = campaignContentService.CampaignPut(model);
            System.Console.WriteLine($"result:\n{JsonConvert.SerializeObject(campaignResult)}");

            //Assert
            Assert.IsFalse(campaignResult is null);
            Assert.IsTrue(campaignResult.CompanyId == newCompanyId);
        }
        [Test]
        public async Task CampaignDeleteDeleteTest()
        {
            //Arrange
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();
            var campaignContentId = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            repositoryMock.Setup(p => p.GetAsQueryable<Status>())
                .Returns(new TestAsyncEnumerable<Status>(new List<Status>
                {
                    new Status
                    {
                        StatusName = "Active",
                        StatusId = 3
                    },
                    new Status
                    {
                        StatusName = "Inactive",
                        StatusId = 8
                    }
                }.AsQueryable()));
            var campaignContent = new CampaignContent
            {
                CampaignContentId = campaignContentId
            };
            var campaign = new Campaign
            {
                CampaignId = campaignId,
                CompanyId = companyId,
                CreationDate = DateTime.Now.AddMinutes(-3),
                StatusId = 3,
                CampaignContents = new List<CampaignContent>
                {
                    campaignContent
                }
            };
            requestFiltersMock.Setup(p => p.IsCompanyBelongToUser(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<String>()))
                .Returns(true);
            repositoryMock.Setup(p => p.GetAsQueryable<Campaign>())
                .Returns(new TestAsyncEnumerable<Campaign>(new List<Campaign>
                {
                    campaign
                }.AsQueryable()));

            //Act
            var result = campaignContentService.CampaignDelete(campaignId);            
            System.Console.WriteLine(result);

            //Assert
            Assert.IsTrue(result == "Deleted");
        }
        [Test]
        public async Task ContentGetGetTest()
        {
            //Arrange
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();
            var campaignContentId = Guid.NewGuid();
            var contentId = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Supervisor");
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            repositoryMock.Setup(p => p.GetAsQueryable<Status>())
                .Returns(new TestAsyncEnumerable<Status>(new List<Status>
                {
                    new Status
                    {
                        StatusName = "Active",
                        StatusId = 3
                    },
                    new Status
                    {
                        StatusName = "Inactive",
                        StatusId = 8
                    }
                }.AsQueryable()));
            var companyIds = new List<Guid>{companyId};
            requestFiltersMock.Setup(p => p.CheckRolesAndChangeCompaniesInFilter(ref companyIds, It.IsAny<List<Guid>>(), It.IsAny<string>(), It.IsAny<Guid>()));
            repositoryMock.Setup(p => p.GetAsQueryable<Content>())
                .Returns(new TestAsyncEnumerable<Content>(new List<Content>
                {
                    new Content
                    {
                        StatusId = 3,
                        ContentId = contentId,
                        IsTemplate = true,
                        CompanyId = companyId
                    }
                }.AsQueryable()));
            fileRefUtils.Setup(p => p.GetFileLink(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns($"https://Pictures.com/TestPicture.png");

            //Act
            var result = await campaignContentService.ContentGet(
                new List<Guid>{companyId},
                new List<Guid>{corporationId},
                false,
                true,
                true);            
            System.Console.WriteLine(JsonConvert.SerializeObject(result));
            var listOfScreenShotModel = (List<ContentWithScreenshotModel>)result;

            //Assert
            Assert.IsFalse(listOfScreenShotModel is null);
            Assert.IsTrue(listOfScreenShotModel.Count > 0);
        }
        [Test]
        public async Task ContentPostPostTest()
        {
            //Arrange
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();
            var campaignContentId = Guid.NewGuid();
            var contentId = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Supervisor");
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            repositoryMock.Setup(p => p.GetAsQueryable<Status>())
                .Returns(new TestAsyncEnumerable<Status>(new List<Status>
                {
                    new Status
                    {
                        StatusName = "Active",
                        StatusId = 3
                    },
                    new Status
                    {
                        StatusName = "Inactive",
                        StatusId = 8
                    }
                }.AsQueryable()));
            sftpClient.Setup(p => p.DeleteFileIfExistsAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(0));
            sftpClient.Setup(p => p.UploadAsMemoryStreamAsync(It.IsAny<Stream>(), It.IsAny<string>(),It.IsAny<string>(),It.IsAny<bool>()))
                .Returns(Task.FromResult(0));
            fileRefUtils.Setup(p => p.GetFileLink(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns($"https://Pictures.com/TestPicture.png");
            var content = new Content
            {
                StatusId = 3,
                ContentId = contentId,
                IsTemplate = true,
                CompanyId = companyId
            };

            var formData = new FormCollection
            (
                new Dictionary<string, StringValues>
                {
                    {"data", new StringValues(JsonConvert.SerializeObject(content))}
                },
                new FormFileCollection
                {
                    new FormFile(new MemoryStream(), 100, 100, "testName", "testFile")
                }
            );
            requestFiltersMock.Setup(p => p.IsCompanyBelongToUser(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<String>()))
                .Returns(true);

            //Act
            var result = await campaignContentService.ContentPost(formData);

            //Assert
            Assert.IsFalse(result is null);
            Assert.IsTrue(result.ContentId == contentId);
        }
        [Test]
        public async Task ContentPutPutTest()
        {
            //Arrange
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();
            var campaignContentId = Guid.NewGuid();
            var contentId = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Supervisor");
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            
            var content = new Content
            {
                StatusId = 3,
                ContentId = contentId,
                IsTemplate = true,
                CompanyId = companyId
            };
            repositoryMock.Setup(p => p.GetAsQueryable<Content>())
                .Returns(new TestAsyncEnumerable<Content>(new List<Content>
                {
                    content
                }.AsQueryable()));  
            sftpClient.Setup(p => p.DeleteFileIfExistsAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(0));
            sftpClient.Setup(p => p.UploadAsMemoryStreamAsync(It.IsAny<Stream>(), It.IsAny<string>(),It.IsAny<string>(),It.IsAny<bool>()))
                .Returns(Task.FromResult(0));
            var formData = new FormCollection
            (
                new Dictionary<string, StringValues>
                {
                    {"data", new StringValues(JsonConvert.SerializeObject(content))}
                },
                new FormFileCollection
                {
                    new FormFile(new MemoryStream(), 100, 100, "testName", "testFile")
                }
            );

            //Act
            var result = await campaignContentService.ContentPut(formData);

            //Assert
            Assert.IsFalse(result is null);
            Assert.IsTrue(result.ContentId == contentId);
        }
        [Test]
        public async Task ContentDeleteDelete()
        {
            //Arrange 
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();
            var campaignContentId = Guid.NewGuid();
            var contentId = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Supervisor");
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            repositoryMock.Setup(p => p.GetAsQueryable<Status>())
                .Returns(new TestAsyncEnumerable<Status>(new List<Status>
                {
                    new Status
                    {
                        StatusName = "Active",
                        StatusId = 3
                    },
                    new Status
                    {
                        StatusName = "Inactive",
                        StatusId = 8
                    }
                }.AsQueryable()));
            var content = new Content
            {
                StatusId = 3,
                ContentId = contentId,
                IsTemplate = true,
                CompanyId = companyId,
                CampaignContents = new List<CampaignContent>
                {
                    new CampaignContent
                    {
                        StatusId = 3,
                        CampaignContentId = campaignContentId,
                    }
                }
            };
            repositoryMock.Setup(p => p.GetAsQueryable<Content>())
                .Returns(new TestAsyncEnumerable<Content>(new List<Content>
                {
                    content
                }.AsQueryable()));  
            requestFiltersMock.Setup(p => p.IsCompanyBelongToUser(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<String>()))
                .Returns(true);
            repositoryMock.Setup(p => p.Delete<CampaignContent>(It.IsAny<List<CampaignContent>>()));
            repositoryMock.Setup(p => p.Delete(It.IsAny<CampaignContent>()));
            repositoryMock.Setup(p => p.Save());
            sftpClient.Setup(p => p.DeleteFileIfExistsAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(0));

            //Act
            var result = await campaignContentService.ContentDelete(contentId);

            //Assert
            Assert.IsTrue(result == "Removed");
        }
        [Test]
        public async Task GetResponseHeaders()
        {
            //Act
            var result = await campaignContentService.GetResponseHeaders("https://www.heedbook.com/");
            
            //Assert
            Assert.IsFalse(result is null);
        }
    }
}