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
using System.Linq.Expressions;
using UserOperations.Controllers;
using Newtonsoft.Json.Linq;
using UserOperations.Models.Session;

namespace ApiTests
{
    public class UserControllerTests : ApiServiceTest
    {
        private UserService userService;
        private CompanyService companyService;
        private PhraseService phraseService;
        private DialogueService dialogueService;
        [SetUp]
        public new void Setup()
        {
            base.Setup();
            userService = new UserService(
                repositoryMock.Object,
                moqILoginService.Object,
                configMock.Object,
                sftpClient.Object,
                fileRefUtils.Object,
                requestFiltersMock.Object,
                mailSenderMock.Object);
            companyService = new CompanyService(
                repositoryMock.Object,
                moqILoginService.Object,
                requestFiltersMock.Object);
            phraseService = new PhraseService(
                repositoryMock.Object, 
                moqILoginService.Object);
            dialogueService = new DialogueService(
                repositoryMock.Object,
                moqILoginService.Object,
                configMock.Object,
                fileRefUtils.Object,
                requestFiltersMock.Object);
        }
        [Test]
        public async Task GetUsersForDeviceAsyncGetTest()
        {
            //Arrange
            var deviceTypeId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var deviceid = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            repositoryMock.Setup(p => p.FindOrExceptionOneByConditionAsync<ApplicationRole>(It.IsAny<Expression<Func<ApplicationRole, bool>>>()))
                .Returns(Task.FromResult(new ApplicationRole
                    {
                        Id = roleId,
                        Name = "Employee"                        
                    }
                ));
            repositoryMock.Setup(p => p.GetAsQueryable<ApplicationUser>())
                .Returns(new TestAsyncEnumerable<ApplicationUser>(
                    new List<ApplicationUser>
                    {
                        new ApplicationUser()
                        {
                            Id = Guid.NewGuid(),
                            FullName = "TestUser",
                            CreationDate = DateTime.Now,
                            UserRoles = new List<ApplicationUserRole>
                            {
                                new ApplicationUserRole
                                {
                                    RoleId = roleId,

                                }
                            },
                            NormalizedEmail = "HornAndHoves2@heedbook.com".ToUpper(),
                            CompanyId = companyId,
                            Company = new Company
                            {
                                CompanyId = companyId
                            },
                            StatusId = 3,
                            Avatar = "avatar.jpg"
                        }
                    })
                    .AsQueryable());
            fileRefUtils.Setup(p => p.GetFileLink(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns("fileRef");
            fileRefUtils.Setup(p => p.GetFileLink(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns("fileRef");

            //Act
            var result = await userService.GetUsersForDeviceAsync();
            //Assert
            Assert.IsFalse(result is null);
            Assert.IsTrue(result.Count() > 0);
        }
        [Test]
        public async Task GetUsersGetTest()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var deviceid = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Supervisor");
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            moqILoginService.Setup(p => p.GetCurrentCorporationId())
                .Returns(corporationId);
            moqILoginService.Setup(p => p.GetCurrentUserId())
                .Returns(userId);
            repositoryMock.Setup(p => p.GetAsQueryable<ApplicationUser>())
                .Returns(new TestAsyncEnumerable<ApplicationUser>(
                    new List<ApplicationUser>
                    {
                        new ApplicationUser()
                        {
                            Id = Guid.NewGuid(),
                            FullName = "TestUser",
                            CreationDate = DateTime.Now,
                            UserRoles = new List<ApplicationUserRole>
                            {
                                new ApplicationUserRole
                                {
                                    RoleId = roleId,

                                }
                            },
                            NormalizedEmail = "HornAndHoves2@heedbook.com".ToUpper(),
                            CompanyId = companyId,
                            Company = new Company
                            {
                                CompanyId = companyId,
                                CorporationId = corporationId
                            },
                            StatusId = 3,
                            Avatar = "avatar.jpg",
                            
                        }
                    })
                    .AsQueryable());

            //Act
            var result = await userService.GetUsers();

            //Assert
            Assert.IsFalse(result is null);
            Assert.IsTrue(result.Count() > 0);
        }
        [Test]
        public async Task CreateUserWithAvatarAsyncPostTest()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var deviceid = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Supervisor");
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            moqILoginService.Setup(p => p.GetCurrentCorporationId())
                .Returns(corporationId);
            repositoryMock.Setup(p => p.FindOrNullOneByConditionAsync<ApplicationUser>(It.IsAny<Expression<Func<ApplicationUser,bool>>>()))
                .Returns(Task.FromResult<ApplicationUser>(null));
            repositoryMock.Setup(p => p.GetAsQueryable<ApplicationUser>())
                .Returns(new TestAsyncEnumerable<ApplicationUser>(new List<ApplicationUser>
                {
                    new ApplicationUser
                    {
                        Id = userId,
                        CompanyId = companyId,
                        UserRoles = new List<ApplicationUserRole>
                        {
                            new ApplicationUserRole
                            {
                                RoleId = roleId,
                                Role = new ApplicationRole
                                {
                                    Id = roleId
                                }
                            }
                        },
                        StatusId = 3,
                        FullName = "Test Test Test",
                        Avatar = "avatar.jpg"
                    }
                }.AsQueryable()));
            repositoryMock.Setup(p => p.GetAsQueryable<ApplicationRole>())
                .Returns(new TestAsyncEnumerable<ApplicationRole>(new List<ApplicationRole>
                {
                    new ApplicationRole
                    {
                        Id = roleId,
                        Name = "Employee"
                    }
                }.AsQueryable()));
            repositoryMock.Setup(p => p.FindAllAsync<ApplicationRole>())
                .Returns(Task.FromResult(new List<ApplicationRole>
                {
                    new ApplicationRole
                    {
                        Id = roleId,
                        Name = "Employee"
                    }
                }.AsEnumerable()));
            repositoryMock.Setup(p => p.FindOrNullOneByConditionAsync<ApplicationRole>(It.IsAny<Expression<Func<ApplicationRole, bool>>>()))
                .Returns(Task.FromResult(
                    new ApplicationRole
                    {
                        Id = roleId,
                        Name = "Employee"
                    }
                ));
            // repositoryMock.Setup(p => p.FindOrNullOneByConditionAsync<ApplicationRole>(It.IsAny<Expression<Func<ApplicationRole, bool>>>()))
            //     .Returns(Task.FromResult(
            //          new ApplicationRole
            //         {
            //             Id = roleId,
            //             Name = "Employee"
            //         }
            //     ));
            repositoryMock.Setup(p => p.FindByConditionAsync<ApplicationUserRole>(It.IsAny<Expression<Func<ApplicationUserRole, bool>>>()))
                .Returns(Task.FromResult(
                     new List<ApplicationUserRole>
                     {
                         new ApplicationUserRole
                         {
                             RoleId = roleId
                         }
                     }.AsEnumerable()
                ));
            repositoryMock.Setup(p => p.Delete<ApplicationUserRole>(It.IsAny<Expression<Func<ApplicationUserRole, bool>>>()));
            requestFiltersMock.Setup(p => p.IsCompanyBelongToUser(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<String>()))
                .Returns(true);
            
            repositoryMock.Setup(p => p.SaveAsync())
                .Returns(Task.FromResult(0));
            sftpClient.Setup(p => p.UploadAsMemoryStreamAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(Task.FromResult(0));
            fileRefUtils.Setup(p => p.GetFileLink(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns("fileRef");
            mailSenderMock.Setup(p => p.SendUserRegisterEmail(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .Returns(Task.FromResult(0));
            var formData = new FormCollection
            (
                new Dictionary<string, StringValues>
                {
                    {"data", new StringValues(JsonConvert.SerializeObject(new PostUser
                    {
                        FullName = "testName",
                        Email = "test2@heedbook.com",
                        EmployeeId = Guid.NewGuid().ToString(),
                        RoleId = roleId,
                        Password = "TestPassword",
                        CompanyId = companyId

                    }))}
                },
                new FormFileCollection
                {
                    new FormFile(new MemoryStream(), 100, 100, "testName", "testFile")
                }
            );

            //Act
            var result = await userService.CreateUserWithAvatarAsync(formData);

            //Assert
            Assert.IsFalse(result is null);
        }
        [Test]
        public async Task EditUserWithAvatarAsyncPutTest()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var deviceid = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Supervisor");
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            moqILoginService.Setup(p => p.GetCurrentCorporationId())
                .Returns(corporationId);
            repositoryMock.Setup(p => p.FindOrNullOneByConditionAsync<ApplicationRole>(It.IsAny<Expression<Func<ApplicationRole, bool>>>()))
                .Returns(Task.FromResult(
                    new ApplicationRole
                    {
                        Id = roleId,
                        Name = "Employee"
                    }
                ));
            requestFiltersMock.Setup(p => p.IsCompanyBelongToUser(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<String>()))
                .Returns(true);
            repositoryMock.Setup(p => p.FindAllAsync<ApplicationRole>())
                .Returns(Task.FromResult(new List<ApplicationRole>
                {
                    new ApplicationRole
                    {
                        Id = roleId,
                        Name = "Employee"
                    }
                }.AsEnumerable()));
            repositoryMock.Setup(p => p.Create<ApplicationUser>(It.IsAny<ApplicationUser>()));
            repositoryMock.Setup(p => p.SaveAsync())
                .Returns(Task.FromResult(0));
            sftpClient.Setup(p => p.UploadAsMemoryStreamAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(Task.FromResult(0));
            repositoryMock.Setup(p => p.GetAsQueryable<ApplicationUser>())
                .Returns(new TestAsyncEnumerable<ApplicationUser>(new List<ApplicationUser>
                {
                    new ApplicationUser
                    {
                        Id = userId,
                        CompanyId = companyId,
                        UserRoles = new List<ApplicationUserRole>
                        {
                            new ApplicationUserRole
                            {
                                RoleId = roleId,
                                Role = new ApplicationRole
                                {
                                    Id = roleId
                                }
                            }
                        },
                        StatusId = 3,
                        FullName = "Test Test Test",
                        Avatar = "avatar.jpg"
                    }
                }.AsQueryable()));
            mailSenderMock.Setup(p => p.SendUserRegisterEmail(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .Returns(Task.FromResult(0));

            var formData = new FormCollection
            (
                new Dictionary<string, StringValues>
                {
                    {"data", new StringValues(JsonConvert.SerializeObject(new PostUser
                    {
                        FullName = "testName",
                        Email = "test2@heedbook.com",
                        EmployeeId = Guid.NewGuid().ToString(),
                        RoleId = roleId,
                        Password = "TestPassword",
                        CompanyId = companyId

                    }))}
                },
                new FormFileCollection
                {
                    new FormFile(new MemoryStream(), 100, 100, "testName", "testFile")
                }
            );

            //Act
            var result = await userService.CreateUserWithAvatarAsync(formData);

            //Assert
            Assert.IsFalse(result is null);
        }
        [Test]
        public async Task DeleteUserWithAvatarAsyncDeleteTest()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var deviceid = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Supervisor");
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            moqILoginService.Setup(p => p.GetCurrentCorporationId())
                .Returns(corporationId);
            repositoryMock.Setup(p => p.GetAsQueryable<ApplicationUser>())
                .Returns(new TestAsyncEnumerable<ApplicationUser>(new List<ApplicationUser>
                {
                    new ApplicationUser
                    {
                        Id = userId,
                        CompanyId = companyId,
                        UserRoles = new List<ApplicationUserRole>
                        {
                            new ApplicationUserRole
                            {
                                RoleId = roleId,
                                Role = new ApplicationRole
                                {
                                    Id = roleId
                                }
                            }
                        },
                        StatusId = 3,
                        FullName = "Test Test Test",
                        Avatar = "avatar.jpg"
                    }
                }.AsQueryable()));
            repositoryMock.Setup(p => p.FindOrNullOneByConditionAsync<ApplicationRole>(It.IsAny<Expression<Func<ApplicationRole, bool>>>()))
                .Returns(Task.FromResult(
                    new ApplicationRole
                    {
                        Id = roleId,
                        Name = "Employee"
                    }
                ));
            requestFiltersMock.Setup(p => p.IsCompanyBelongToUser(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<String>()))
                .Returns(true);
            repositoryMock.Setup(p => p.SaveAsync())
                .Returns(Task.FromResult(0));
            repositoryMock.Setup(p => p.Delete<ApplicationUserRole>(It.IsAny<ApplicationUserRole>()));
            repositoryMock.Setup(p => p.Delete<ApplicationUser>(It.IsAny<ApplicationUser>()));
            requestFiltersMock.Setup(p => p.IsCompanyBelongToUser(It.IsAny<Guid>()))
                .Returns(true);

            //Act
            var result = await userService.DeleteUserWithAvatarAsync(userId);

            //Assert
            Assert.IsFalse(result is null);
            Assert.IsTrue(result == "Deleted");
        }
        [Test]
        public async Task GetCompaniesForSupervisorAsync()
        {
            //Arrange
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            repositoryMock.Setup(p => p.GetWithInclude<Company>(It.IsAny<Expression<Func<Company, bool>>>(), It.IsAny<Expression<Func<Company, object>>[]>()))
                .Returns(new TestAsyncEnumerable<Company>(new List<Company>
                {
                    new Company
                    {
                        CorporationId = corporationId,
                        StatusId = 3,
                        WorkingTimes = new List<WorkingTime>
                        {
                            new WorkingTime
                            {
                                Day = 1,
                                CompanyId = companyId,
                                BegTime = DateTime.Now.Date.AddHours(-3),
                                EndTime = DateTime.Now.Date.AddHours(3)
                            }
                        }
                    }                    
                }.AsQueryable()));

            //Act
            var result = companyService.GetCompaniesForSupervisorAsync(corporationId);

            //Assert
            Assert.IsFalse(result is null);
            Assert.IsTrue(result.Count() > 0);
        }
        [Test]
        public async Task GetCompaniesForAdminGetTest()
        {
            //Arrange
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            repositoryMock.Setup(p => p.GetWithInclude<Company>(It.IsAny<Expression<Func<Company, bool>>>(), It.IsAny<Expression<Func<Company, object>>[]>()))
                .Returns(new TestAsyncEnumerable<Company>(new List<Company>
                {
                    new Company
                    {
                        CorporationId = corporationId,
                        StatusId = 3,
                        WorkingTimes = new List<WorkingTime>
                        {
                            new WorkingTime
                            {
                                Day = 1,
                                CompanyId = companyId,
                                BegTime = DateTime.Now.Date.AddHours(-3),
                                EndTime = DateTime.Now.Date.AddHours(3)
                            }
                        }
                    }                    
                }.AsQueryable()));

            //Act
            var result = companyService.GetCompaniesForAdmin();

            //Assert
            Assert.IsFalse(result is null);
            Assert.IsTrue(result.Count() > 0);
        }
        [Test]
        public async Task GetCompanyByIdAsyncGetTest()
        {
            //Arrange
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            repositoryMock.Setup(p => p.GetWithIncludeOne<Company>(It.IsAny<Expression<Func<Company, bool>>>(), It.IsAny<Expression<Func<Company, object>>[]>()))
                .Returns(
                    new Company
                    {
                        CorporationId = corporationId,
                        StatusId = 3,
                        WorkingTimes = new List<WorkingTime>
                        {
                            new WorkingTime
                            {
                                Day = 1,
                                CompanyId = companyId,
                                BegTime = DateTime.Now.Date.AddHours(-3),
                                EndTime = DateTime.Now.Date.AddHours(3)
                            }
                        }
                    });

            //Act
            var result = companyService.GetCompanyByIdAsync(companyId);

            //Assert
            Assert.IsFalse(result is null);
        }
        [Test]
        public async Task CompanysPostAsyncPostTest()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var deviceid = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Supervisor");
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            repositoryMock.Setup(p => p.Create<Company>(It.IsAny<Company>()));
            repositoryMock.Setup(p => p.SaveAsync())
                .Returns(Task.FromResult(0));
            repositoryMock.Setup(p => p.CreateAsync<WorkingTime>(It.IsAny<WorkingTime>()))
                .Returns(Task.FromResult(0));
            var model = new Company
            {
                CompanyId = companyId
            };

            //Act
            var result = companyService.AddNewCompanyAsync(model, corporationId);

            //Assert
            Assert.IsFalse(result is null);
        }
        [Test]
        public async Task UpdateCompanAsyncPutTest()
        {
            //Arrange
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Supervisor");
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            repositoryMock.Setup(p => p.GetWithIncludeOne<Company>(It.IsAny<Expression<Func<Company, bool>>>(), It.IsAny<Expression<Func<Company, object>>[]>()))
                .Returns(
                    new Company
                    {
                        CompanyId = companyId,
                        CorporationId = corporationId,
                        StatusId = 3,
                        WorkingTimes = new List<WorkingTime>
                        {
                            new WorkingTime
                            {
                                Day = 1,
                                CompanyId = companyId,
                                BegTime = DateTime.Now.Date.AddHours(-3),
                                EndTime = DateTime.Now.Date.AddHours(3)
                            }
                        }
                    });
            repositoryMock.Setup(p => p.SaveAsync())
                .Returns(Task.FromResult(0));
            var workingTimes = new List<WorkingTime>
            {
                new WorkingTime
                {
                    Day = 1,
                    CompanyId = companyId,
                    BegTime = DateTime.Now.Date.AddHours(-3),
                    EndTime = DateTime.Now.Date.AddHours(3)
                }
            };

            //Act
            var result = companyService.UpdateCompanAsync(workingTimes);

            //Assert
            Assert.IsFalse(result is null);
        }
        [Test]
        public async Task GetCorporationAsyncGetTest()
        {
            //Arrange
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Admin");
            repositoryMock.Setup(p => p.FindAllAsync<Corporation>())
                .Returns(Task.FromResult(
                    new List<Corporation>
                    {
                        new Corporation
                        {
                            Id = corporationId
                        }                    
                    }.AsEnumerable()));

            //Act
            var result = await companyService.GetCorporationAsync();

            //Assert
            Assert.IsFalse(result is null);
            Assert.IsTrue(result.Count() > 0);
        }
        [Test]
        public async Task PhraseLibGet1Test()
        {
            //Arrange
            var companyId = Guid.NewGuid();
            var isTemplate = true;
            var languageId = 2;
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            moqILoginService.Setup(p => p.GetCurrentLanguagueId())
                .Returns(languageId);
            repositoryMock.Setup(p => p.GetAsQueryable<PhraseCompany>())
                .Returns(new TestAsyncEnumerable<PhraseCompany>(new List<PhraseCompany>
                {
                    new PhraseCompany
                    {
                        CompanyId = companyId,
                        Phrase = new Phrase
                        {
                            PhraseId = Guid.NewGuid(),
                            IsTemplate = isTemplate,
                            PhraseText = "TestPhrase",
                            LanguageId = languageId
                        }
                    }                    
                }.AsQueryable()));

            //Act
            var result = await phraseService.GetPhraseIdsByCompanyIdAsync(isTemplate);

            //Assert
            Assert.IsFalse(result is null);
            Assert.IsTrue(result.Count > 0);
        }
        [Test]
        public async Task PhraseLibGet2Test()
        {
            //Arrange
            var phraseId = Guid.NewGuid();
            var isTemplate = true;
            var languageId = 2;
            moqILoginService.Setup(p => p.GetCurrentLanguagueId())
                .Returns(languageId);
            repositoryMock.Setup(p => p.GetAsQueryable<Phrase>())
                .Returns(new TestAsyncEnumerable<Phrase>(new List<Phrase>
                {
                    new Phrase
                    {
                        PhraseId = phraseId,
                        IsTemplate = isTemplate,
                        PhraseText = "TestPhrase",
                        LanguageId = languageId
                    }                    
                }.AsQueryable()));

            //Act            
            var result = await phraseService.GetPhrasesNotBelongToCompanyByIdsAsync(
                new List<Guid>{Guid.NewGuid()},
                isTemplate);
            System.Console.WriteLine(JsonConvert.SerializeObject(result));

            //Assert
            Assert.IsFalse(result is null);
            Assert.IsTrue(result.Count > 0);
        }
        [Test]
        public async Task PhraseLibPostTest()
        {
            //Arrange
            var phraseId = Guid.NewGuid();
            var phraseTypeId = Guid.NewGuid();
            var salesStageId = Guid.NewGuid();
            var isTemplate = true;
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var languageId = 2;
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            moqILoginService.Setup(p => p.GetCurrentLanguagueId())
                .Returns(languageId);
            moqILoginService.Setup(p => p.GetCurrentCorporationId())
                .Returns(corporationId);
            repositoryMock.Setup(p => p.GetAsQueryable<Phrase>())
                .Returns(new TestAsyncEnumerable<Phrase>(new List<Phrase>
                {
                    new Phrase
                    {
                        PhraseId = phraseId,
                        PhraseTypeId = phraseTypeId,
                        IsTemplate = isTemplate,
                        PhraseText = "testPhrase",
                        LanguageId = languageId,
                        PhraseCompanys = new List<PhraseCompany>
                        {
                            new PhraseCompany
                            {
                                PhraseCompanyId = Guid.NewGuid(),
                                PhraseId = phraseId,
                                CompanyId = companyId
                            }
                        }
                    }                    
                }.AsQueryable()));
            repositoryMock.Setup(p => p.CreateAsync<Phrase>(It.IsAny<Phrase>()))
                .Returns(Task.FromResult(0));
            repositoryMock.Setup(p => p.FindOrNullOneByConditionAsync<PhraseCompany>(It.IsAny<Expression<Func<PhraseCompany, bool>>>()))
                .Returns(Task.FromResult(
                    new PhraseCompany
                    {
                        PhraseCompanyId = Guid.NewGuid(),
                        PhraseId = phraseId,
                        CompanyId = companyId
                    }
                ));
            repositoryMock.Setup(p => p.CreateAsync<PhraseCompany>(It.IsAny<PhraseCompany>()))
                .Returns(Task.FromResult(0));
            var model = new PhrasePost
            {
                PhraseText = "testPhrase",
                PhraseTypeId = phraseTypeId,
                LanguageId = 2,
                SalesStageId = salesStageId,
                WordsSpace = 2,
                Accurancy = 0.6d,
                IsTemplate = isTemplate
            };

            //Act
            var result = await phraseService.CreateNewPhrasAsync(model);

            //Assert
            Assert.IsFalse(result is null);
        }
        [Test]
        public async Task PhraseLibPut1Test()
        {
            //Arrange
            var companyId = Guid.NewGuid();
            var phraseId = Guid.NewGuid();
            var isTemplate = true;
            var languageId = 2;
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            repositoryMock.Setup(p => p.GetAsQueryable<PhraseCompany>())
                .Returns(new TestAsyncEnumerable<PhraseCompany>(new List<PhraseCompany>
                {
                    new PhraseCompany
                    {
                        CompanyId = companyId,
                        Phrase = new Phrase
                        {
                            PhraseId = phraseId,
                            IsTemplate = isTemplate,
                            PhraseText = "TestPhrase",
                            LanguageId = languageId
                        }
                    }                    
                }.AsQueryable()));

            //Act
            var result = await phraseService.GetPhraseInCompanyByIdAsync(phraseId, isTemplate);

            //Assert
            Assert.IsFalse(result is null);
        }
        [Test]
        public async Task PhraseLibPut2Test()
        {
            //Arrange
            var companyId = Guid.NewGuid();
            var phraseId = Guid.NewGuid();
            var isTemplate = true;
            var languageId = 2;
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            repositoryMock.Setup(p => p.GetAsQueryable<PhraseCompany>())
                .Returns(new TestAsyncEnumerable<PhraseCompany>(new List<PhraseCompany>
                {
                    new PhraseCompany
                    {
                        CompanyId = companyId,
                        Phrase = new Phrase
                        {
                            PhraseId = Guid.NewGuid(),
                            IsTemplate = isTemplate,
                            PhraseText = "TestPhrase",
                            LanguageId = languageId
                        }
                    }                    
                }.AsQueryable()));
            repositoryMock.Setup(p => p.Update<Phrase>(It.IsAny<Phrase>()));
            repositoryMock.Setup(p => p.SaveAsync())
                .Returns(Task.FromResult(0));
            var phrase1 = new Phrase
            {
                PhraseId = Guid.NewGuid(),
                PhraseText = "testPhrase"
            };
            var phrase2 = new Phrase
            {
                PhraseId = Guid.NewGuid(),
                PhraseText = "testPhrase"
            };

            //Act
            var result = await phraseService.EditAndSavePhraseAsync(phrase1, phrase2);

            //Assert
            Assert.IsFalse(result is null);
        }
        [Test]
        public async Task PhraseLibDelete1Test()
        {
            //Arrange
            var companyId = Guid.NewGuid();
            var phraseId = Guid.NewGuid();
            var isTemplate = true;
            var languageId = 2;
            repositoryMock.Setup(p => p.GetAsQueryable<Phrase>())
                .Returns(new TestAsyncEnumerable<Phrase>(new List<Phrase>
                {
                    new Phrase
                    {
                        PhraseId = phraseId,
                        IsTemplate = isTemplate,
                        PhraseText = "TestPhrase",
                        LanguageId = languageId,
                        PhraseCompanys = new List<PhraseCompany>{}
                    }                    
                }.AsQueryable()));

            //Act
            var result = await phraseService.GetPhraseByIdAsync(phraseId);

            //Assert
            Assert.IsFalse(result is null);
        }
        [Test]
        public async Task PhraseLibDelete2Test()
        {
            //Arrange
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var phraseId = Guid.NewGuid();
            var isTemplate = false;
            var languageId = 2;
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            moqILoginService.Setup(p => p.GetCurrentCorporationId())
                .Returns(corporationId);
            repositoryMock.Setup(p => p.Delete<PhraseCompany>(It.IsAny<List<PhraseCompany>>()));
            repositoryMock.Setup(p => p.Delete<SalesStagePhrase>(It.IsAny<SalesStagePhrase>()));
            repositoryMock.Setup(p => p.Delete<Phrase>(It.IsAny<Phrase>()));
            repositoryMock.Setup(p => p.SaveAsync())
                .Returns(Task.FromResult(0));
            var model = new Phrase
            {
                PhraseId = phraseId,
                IsTemplate = isTemplate,
                PhraseText = "TestPhrase",
                LanguageId = languageId,
                PhraseCompanys = new List<PhraseCompany>
                {
                    new PhraseCompany
                    {
                        CompanyId = companyId
                    }
                },
                SalesStagePhrases = new List<SalesStagePhrase>
                {
                    new SalesStagePhrase
                    {
                        CompanyId = companyId,
                        CorporationId = corporationId
                    }
                }
            };   

            //Act
            var result = await phraseService.DeleteAndSavePhraseWithPhraseCompanyAsync(model);

            //Assert
            Assert.IsFalse(result is null);
            Assert.IsTrue(result == "Deleted from PhraseCompany and Phrases");
        }
        [Test]
        public async Task CompanyPhraseGetTest()
        {
            //Arrange
            var companyId = Guid.NewGuid();
            var isTemplate = false;
            var languageId = 2;
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            repositoryMock.Setup(p => p.GetAsQueryable<PhraseCompany>())
                .Returns(new TestAsyncEnumerable<PhraseCompany>(new List<PhraseCompany>
                {
                    new PhraseCompany
                    {
                        CompanyId = companyId,
                        Phrase = new Phrase
                        {
                            PhraseId = Guid.NewGuid(),
                            IsTemplate = isTemplate,
                            PhraseText = "TestPhrase",
                            LanguageId = languageId
                        }
                    }                    
                }.AsQueryable()));

            //Act
            var result = await phraseService.GetPhrasesInCompanyByIdsAsync(new List<Guid>{companyId});

            //Assert
            Assert.IsFalse(result is null);
            Assert.IsTrue(result.Count > 0);
        }
        [Test]
        public async Task CompanyPhrasePostTest()
        {
            //Arrange
            var companyId = Guid.NewGuid();
            var phraseId = Guid.NewGuid();
            var isTemplate = false;
            var languageId = 2;
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            repositoryMock.Setup(p => p.SaveAsync())
                .Returns(Task.FromResult(0));
            repositoryMock.Setup(p => p.FindOrNullOneByConditionAsync<PhraseCompany>(It.IsAny<Expression<Func<PhraseCompany, bool>>>()))
                .Returns(Task.FromResult(
                    new PhraseCompany
                    {
                        PhraseCompanyId = Guid.NewGuid(),
                        PhraseId = phraseId,
                        CompanyId = companyId
                    }
                ));

            //Act
            var result = phraseService.CreateNewPhrasesCompanyAsync(new List<Guid>{phraseId});

            //Assert
            Assert.IsTrue(true);
        }
        [Test]
        public async Task DialogueGetTest()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var phraseId = Guid.NewGuid();
            var phraseTypeId = Guid.NewGuid();
            var clientId = Guid.NewGuid();
            var dateTimeNow = DateTime.Now.Date;
            requestFiltersMock.Setup(p => p.GetBegDate(It.IsAny<string>()))
               .Returns(dateTimeNow.AddDays(-5));
            requestFiltersMock.Setup(p => p.GetEndDate(It.IsAny<string>()))
               .Returns(dateTimeNow);
            var companyIds = new List<Guid>{companyId};
            requestFiltersMock.Setup(p => p.CheckRolesAndChangeCompaniesInFilter(ref companyIds, It.IsAny<List<Guid>>()));
            repositoryMock.Setup(p => p.GetAsQueryable<Dialogue>())
               .Returns(new TestAsyncEnumerable<Dialogue>(
                    new List<Dialogue>()
                    {
                        new Dialogue()
                        {
                            DialogueId = Guid.NewGuid(),
                            ClientId = clientId,
                            CreationTime = DateTime.Now,
                            BegTime = DateTime.Now.Date.AddMinutes(-5),
                            EndTime = DateTime.Now.Date.AddMinutes(-1),
                            DeviceId = deviceId,
                            Device = new Device{
                                DeviceId = deviceId,
                                CompanyId = companyId,
                                Name = "testDevice"
                            },
                            StatusId = 3,
                            InStatistic = true,
                            ApplicationUserId = userId,
                            ApplicationUser = new ApplicationUser()
                            {
                                Id = userId,
                                FullName = "TestFullName"
                            },
                            DialogueClientProfile = new List<DialogueClientProfile>
                            {
                                new DialogueClientProfile
                                {
                                    Age = 20, 
                                    Gender = "male",
                                    Avatar = "avatar.jpg"
                                }
                            },
                            DialoguePhrase = new List<DialoguePhrase>
                            {
                                new DialoguePhrase
                                {
                                    PhraseId = phraseId,
                                    PhraseTypeId = phraseTypeId
                                }
                            },
                            DialogueHint = new List<DialogueHint>
                            {
                                new DialogueHint{}
                            },
                            DialogueClientSatisfaction = new List<DialogueClientSatisfaction>
                            {
                                new DialogueClientSatisfaction
                                {
                                    MeetingExpectationsTotal = 0.7d
                                }
                            }
                        }
                    }));
            fileRefUtils.Setup(p => p.GetFileUrlFast(It.IsAny<string>()))
                .Returns("testUrl");

            //Act
            var result = await dialogueService.GetAllDialogues(
                TestData.beg,
                TestData.end,
                new List<Guid?>{userId},
                new List<Guid>{deviceId},
                new List<Guid>{companyId},
                new List<Guid>{corporationId},
                new List<Guid>{phraseId},
                new List<Guid>{phraseTypeId},
                clientId,
                true);
            
            //Assert
            Assert.IsFalse(result is null);
            Assert.IsTrue(result.Count > 0);
        }
        [Test]
        public async Task DialoguePaginatedGetTest()
        {
            // //Arrange
            // var userId = Guid.NewGuid();
            // var deviceId = Guid.NewGuid();
            // var companyId = Guid.NewGuid();
            // var corporationId = Guid.NewGuid();
            // var phraseId = Guid.NewGuid();
            // var phraseTypeId = Guid.NewGuid();
            // var clientId = Guid.NewGuid();
            // var dateTimeNow = DateTime.Now.Date;
            // requestFiltersMock.Setup(p => p.GetBegDate(It.IsAny<string>()))
            //    .Returns(dateTimeNow.AddDays(-5));
            // requestFiltersMock.Setup(p => p.GetEndDate(It.IsAny<string>()))
            //    .Returns(dateTimeNow);
            // var companyIds = new List<Guid>{companyId};
            // requestFiltersMock.Setup(p => p.CheckRolesAndChangeCompaniesInFilter(ref companyIds, It.IsAny<List<Guid>>()));
            // repositoryMock.Setup(p => p.GetAsQueryable<Dialogue>())
            //    .Returns(
            //         new List<Dialogue>()
            //         {
            //             new Dialogue()
            //             {
            //                 DialogueId = Guid.NewGuid(),
            //                 ClientId = clientId,
            //                 CreationTime = DateTime.Now,
            //                 BegTime = DateTime.Now.Date.AddMinutes(-5),
            //                 EndTime = DateTime.Now.Date.AddMinutes(-1),
            //                 DeviceId = deviceId,
            //                 Device = new Device{
            //                     DeviceId = deviceId,
            //                     CompanyId = companyId,
            //                     Name = "testDevice"
            //                 },
            //                 StatusId = 3,
            //                 InStatistic = true,
            //                 ApplicationUserId = userId,
            //                 ApplicationUser = new ApplicationUser()
            //                 {
            //                     Id = userId,
            //                     FullName = "TestFullName"
            //                 },
            //                 DialogueClientProfile = new List<DialogueClientProfile>
            //                 {
            //                     new DialogueClientProfile
            //                     {
            //                         Age = 20, 
            //                         Gender = "male",
            //                         Avatar = "avatar.jpg"
            //                     }
            //                 },
            //                 DialoguePhrase = new List<DialoguePhrase>
            //                 {
            //                     new DialoguePhrase
            //                     {
            //                         PhraseId = phraseId,
            //                         PhraseTypeId = phraseTypeId
            //                     }
            //                 },
            //                 DialogueHint = new List<DialogueHint>
            //                 {
            //                     new DialogueHint{}
            //                 },
            //                 DialogueClientSatisfaction = new List<DialogueClientSatisfaction>
            //                 {
            //                     new DialogueClientSatisfaction
            //                     {
            //                         MeetingExpectationsTotal = 0.7d
            //                     }
            //                 }
            //             },
            //             new Dialogue()
            //             {
            //                 DialogueId = Guid.NewGuid(),
            //                 ClientId = clientId,
            //                 CreationTime = DateTime.Now,
            //                 BegTime = DateTime.Now.Date.AddMinutes(-5),
            //                 EndTime = DateTime.Now.Date.AddMinutes(-1),
            //                 DeviceId = deviceId,
            //                 Device = new Device{
            //                     DeviceId = deviceId,
            //                     CompanyId = companyId,
            //                     Name = "testDevice"
            //                 },
            //                 StatusId = 3,
            //                 InStatistic = true,
            //                 ApplicationUserId = userId,
            //                 ApplicationUser = new ApplicationUser()
            //                 {
            //                     Id = userId,
            //                     FullName = "TestFullName"
            //                 },
            //                 DialogueClientProfile = new List<DialogueClientProfile>
            //                 {
            //                     new DialogueClientProfile
            //                     {
            //                         Age = 20, 
            //                         Gender = "male",
            //                         Avatar = "avatar.jpg"
            //                     }
            //                 },
            //                 DialoguePhrase = new List<DialoguePhrase>
            //                 {
            //                     new DialoguePhrase
            //                     {
            //                         PhraseId = phraseId,
            //                         PhraseTypeId = phraseTypeId
            //                     }
            //                 },
            //                 DialogueHint = new List<DialogueHint>
            //                 {
            //                     new DialogueHint{}
            //                 },
            //                 DialogueClientSatisfaction = new List<DialogueClientSatisfaction>
            //                 {
            //                     new DialogueClientSatisfaction
            //                     {
            //                         MeetingExpectationsTotal = 0.7d
            //                     }
            //                 }
            //             }
            //         }.AsQueryable());
            // fileRefUtils.Setup(p => p.GetFileUrlFast(It.IsAny<string>()))
            //     .Returns("testUrl");
                
            // //Act
            // var result = await dialogueService.GetAllDialoguesPaginated(
            //     TestData.beg,
            //     TestData.end,
            //     new List<Guid?>{userId},
            //     new List<Guid>{deviceId},
            //     new List<Guid>{companyId},
            //     new List<Guid>{corporationId},
            //     new List<Guid>{phraseId},
            //     new List<Guid>{phraseTypeId},
            //     clientId,
            //     true,
            //     1, 0,
            //     "BegTime",
            //     "desc");
            // System.Console.WriteLine(JsonConvert.SerializeObject(result));
            // //Assert
            // Assert.IsFalse(result is null);
        }
        [Test]
        public async Task DialogueIncludeGetTest()
        {
            //Arrange
                              
            var userId = Guid.NewGuid();
            var dialogueId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var phraseId = Guid.NewGuid();
            var phraseTypeId = Guid.NewGuid();
            var clientId = Guid.NewGuid();
            var salesStageId = Guid.NewGuid();
            repositoryMock.Setup(p => p.GetAsQueryable<Company>())
               .Returns(
                   new List<Company>
                   {
                       new Company
                       {
                           CompanyId = companyId,
                           CorporationId = corporationId
                       }
                   }.AsQueryable()
               );
            repositoryMock.Setup(p => p.GetAsQueryable<Dialogue>())
               .Returns(
                    new List<Dialogue>()
                    {
                        new Dialogue()
                        {
                            DialogueId = dialogueId,
                            ClientId = clientId,
                            CreationTime = DateTime.Now,
                            BegTime = DateTime.Now.Date.AddMinutes(-5),
                            EndTime = DateTime.Now.Date.AddMinutes(-1),
                            DeviceId = deviceId,
                            Device = new Device{
                                DeviceId = deviceId,
                                CompanyId = companyId,
                                Name = "testDevice"
                            },
                            StatusId = 3,
                            InStatistic = true,
                            ApplicationUserId = userId,
                            ApplicationUser = new ApplicationUser()
                            {
                                Id = userId,
                                FullName = "TestFullName"
                            },
                            DialogueClientProfile = new List<DialogueClientProfile>
                            {
                                new DialogueClientProfile
                                {
                                    Age = 20, 
                                    Gender = "male",
                                    Avatar = "avatar.jpg"
                                }
                            },
                            DialoguePhrase = new List<DialoguePhrase>
                            {
                                new DialoguePhrase
                                {
                                    PhraseId = phraseId,
                                    PhraseTypeId = phraseTypeId
                                }
                            },
                            DialogueHint = new List<DialogueHint>
                            {
                                new DialogueHint{}
                            },
                            DialogueClientSatisfaction = new List<DialogueClientSatisfaction>
                            {
                                new DialogueClientSatisfaction
                                {
                                    MeetingExpectationsTotal = 0.7d
                                }
                            },
                            DialogueWord = new List<DialogueWord>
                            {
                                new DialogueWord
                                {
                                    DialogueWordId = Guid.NewGuid(),
                                    DialogueId = dialogueId,
                                    IsClient = true,
                                    Words = "test Word"
                                }
                            }                           
                        }
                    }.AsQueryable());
            repositoryMock.Setup(p => p.GetAsQueryable<SalesStage>())
               .Returns(new List<SalesStage>
               {
                   new SalesStage
                   {
                       SalesStageId = salesStageId,
                       SequenceNumber = 2,
                       Name = "TestSaleStage",
                       SalesStagePhrases = new List<SalesStagePhrase>
                       {
                           new SalesStagePhrase
                           {
                               CompanyId = companyId,
                               CorporationId = corporationId,
                               PhraseId = phraseId
                           }
                       }
                   }
               }.AsQueryable());
            fileRefUtils.Setup(p => p.GetFileUrlFast(It.IsAny<string>()))
                .Returns("testUrl");
            
            //Act
            var result = await dialogueService.DialogueGet(dialogueId);

            //Assert
            Assert.IsFalse(result is null);
        }
        [Test]
        public async Task DialoguePutTest()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var dialogueId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            var phraseId = Guid.NewGuid();
            var phraseTypeId = Guid.NewGuid();
            var clientId = Guid.NewGuid();
            var salesStageId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            repositoryMock.Setup(p => p.SaveAsync())
                .Returns(Task.FromResult(0));
            repositoryMock.Setup(p => p.GetAsQueryable<Dialogue>())
               .Returns(
                    new List<Dialogue>()
                    {
                        new Dialogue()
                        {
                            DialogueId = dialogueId,
                            ClientId = clientId,
                            CreationTime = DateTime.Now,
                            BegTime = DateTime.Now.Date.AddMinutes(-5),
                            EndTime = DateTime.Now.Date.AddMinutes(-1),
                            DeviceId = deviceId,
                            Device = new Device{
                                DeviceId = deviceId,
                                CompanyId = companyId,
                                Name = "testDevice"
                            },
                            StatusId = 3,
                            InStatistic = true,
                            ApplicationUserId = userId,
                            ApplicationUser = new ApplicationUser()
                            {
                                Id = userId,
                                FullName = "TestFullName"
                            },
                            DialogueClientProfile = new List<DialogueClientProfile>
                            {
                                new DialogueClientProfile
                                {
                                    Age = 20, 
                                    Gender = "male",
                                    Avatar = "avatar.jpg"
                                }
                            },
                            DialoguePhrase = new List<DialoguePhrase>
                            {
                                new DialoguePhrase
                                {
                                    PhraseId = phraseId,
                                    PhraseTypeId = phraseTypeId
                                }
                            },
                            DialogueHint = new List<DialogueHint>
                            {
                                new DialogueHint{}
                            },
                            DialogueClientSatisfaction = new List<DialogueClientSatisfaction>
                            {
                                new DialogueClientSatisfaction
                                {
                                    MeetingExpectationsTotal = 0.7d
                                }
                            }                        
                        }
                    }.AsQueryable());
            var model = new DialoguePut
            {
                DialogueId = dialogueId,
                DialogueIds = new List<Guid>{dialogueId},
                InStatistic = true
            };

            //Act
            var result = await dialogueService.ChangeInStatistic(model);

            //Assert
            Assert.IsTrue(result == model.InStatistic);
        }
        [Test]
        public async Task DialogueSatisfactionPutTest()
        {
            //Arrange
            var dialogueId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var deviceid = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Supervisor");
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            moqILoginService.Setup(p => p.GetCurrentCorporationId())
                .Returns(corporationId);
            repositoryMock.Setup(p => p.FindOrExceptionOneByConditionAsync<DialogueClientSatisfaction>(It.IsAny<Expression<Func<DialogueClientSatisfaction,bool>>>()))
               .Returns(Task.FromResult(new DialogueClientSatisfaction
               {
                   MeetingExpectationsTotal = 0.8d,
                   BegMoodByEmpoyee = 0.4d,
                   EndMoodByTeacher = 0.8d,
                   Age = 30,
                   Gender = "male"
               }));
            repositoryMock.Setup(p => p.SaveAsync())
                .Returns(Task.FromResult(0));
            requestFiltersMock.Setup(p => p.IsCompanyBelongToUser(It.IsAny<Guid>()))
                .Returns(true);
            var model = new DialogueSatisfactionPut
            {
                DialogueId = dialogueId,
                Satisfaction = 0.7d,
                BegMoodTotal = 0.5d,
                EndMoodTotal = 0.8d,
                Age = 32,
                Gender = "male"
            };

            //Act
            var result = await dialogueService.SatisfactionChangeByTeacher(model);
            System.Console.WriteLine(JsonConvert.SerializeObject(result));

            //Assert
            Assert.IsFalse(result is null);
        }
        [Test]
        public async Task AlertGetTest()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var dialogueId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var dateTimeNow = DateTime.Now.Date;
            var deviceId = Guid.NewGuid();
            var alertTypeId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            requestFiltersMock.Setup(p => p.GetBegDate(It.IsAny<string>()))
               .Returns(dateTimeNow.AddDays(-5));
            requestFiltersMock.Setup(p => p.GetEndDate(It.IsAny<string>()))
               .Returns(dateTimeNow);
            repositoryMock.Setup(p => p.GetAsQueryable<Dialogue>())
               .Returns(
                    new List<Dialogue>()
                    {
                        new Dialogue()
                        {
                            DialogueId = dialogueId,
                            CreationTime = DateTime.Now,
                            BegTime = DateTime.Now.Date.AddMinutes(-5),
                            EndTime = DateTime.Now.Date.AddMinutes(-1),
                            DeviceId = deviceId,
                            Device = new Device{
                                DeviceId = deviceId,
                                CompanyId = companyId,
                                Name = "testDevice"
                            },
                            StatusId = 3,
                            InStatistic = true,
                            ApplicationUserId = userId                                              
                        }
                    }.AsQueryable());
            repositoryMock.Setup(p => p.GetAsQueryable<Alert>())
               .Returns(
                    new List<Alert>()
                    {
                        new Alert
                        {
                            AlertId = Guid.NewGuid(),
                            CreationDate = dateTimeNow.AddMinutes(-1),
                            DeviceId = deviceId,
                            Device = new Device
                            {
                                DeviceId = deviceId,
                                CompanyId = companyId
                            },
                            AlertTypeId = alertTypeId,
                            ApplicationUserId = userId,
                                                              
                        }
                    }.AsQueryable());

            //Act
            var result = await dialogueService.GetAlerts
            (
                TestData.beg,
                TestData.end,
                new List<Guid?>{userId},
                new List<Guid>{alertTypeId},
                new List<Guid>{deviceId});

            //Assert
            Assert.IsFalse(result is null);
        }
        [Test]
        public async Task VideoMessagePostTest()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var dialogueId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var deviceid = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Employee");
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            moqILoginService.Setup(p => p.GetCurrentCorporationId())
                .Returns(corporationId);
            moqILoginService.Setup(p => p.GetCurrentUserId())
                .Returns(userId);
            var managerRole = new ApplicationRole
            {
                Id = roleId,
                Name = "Manager"
            };
            var supervisorRole = new ApplicationRole
            {
                Id = roleId,
                Name = "Supervisor"
            };
            repositoryMock.Setup(p => p.GetAsQueryable<ApplicationUser>())
               .Returns(
                    new List<ApplicationUser>()
                    {
                        new ApplicationUser
                        {
                            Id = userId,
                            CompanyId = companyId,
                            UserRoles = new List<ApplicationUserRole>
                            {
                                new ApplicationUserRole
                                {
                                    RoleId = roleId,
                                    Role = managerRole
                                }
                            },
                            Company = new Company
                            {
                                CompanyId = companyId,
                                CorporationId = corporationId
                            }
                        }
                    }.AsQueryable());
            repositoryMock.Setup(p => p.GetAsQueryable<ApplicationRole>())
                .Returns(new TestAsyncEnumerable<ApplicationRole>(new List<ApplicationRole>
                {
                    managerRole,
                    supervisorRole
                }.AsQueryable()));
            mailSenderMock.Setup(p => p.SendsEmailsSubscription(It.IsAny<IFormCollection>(), It.IsAny<ApplicationUser>(), It.IsAny<VideoMessage>(), It.IsAny<List<ApplicationUser>>()));
            var formData = new FormCollection
            (
                new Dictionary<string, StringValues>
                {
                    {"data", new StringValues(JsonConvert.SerializeObject(new VideoMessage
                        {
                            Subject = "subject",
                            Body = "<div>body</div>"
                        }))
                    }
                },
                new FormFileCollection
                {
                    new FormFile(new MemoryStream(), 100, 100, "testName", "testFile")
                }
            );

            //Act
            await userService.SendVideoMessageToManager(formData);
            

            //Assert
            Assert.IsTrue(true);
        }
    }
}