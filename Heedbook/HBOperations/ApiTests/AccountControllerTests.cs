using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using UserOperations.Controllers;
using UserOperations.AccountModels;
using HBData.Models.AccountViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using UserOperations.Services;
using HBData.Models;
using System;
using System.Linq.Expressions;
using System.Linq;

namespace ApiTests
{
    public class AccountControllerTests : ApiServiceTest
    {           
        private AccountService accountService;   
        [SetUp]
        public new void Setup()
        {   
            base.Setup();
            accountService = new AccountService(
                moqILoginService.Object, 
                companyServiceMock.Object,
                salesStageServiceMock.Object,
                repositoryMock.Object,
                mailSenderMock.Object,
                spreadSheetDocumentUtils.Object);
        }   
            
        [Test]
        public async Task RegisterPostTest()
        {
            //Arrange          
            repositoryMock.Setup(p => p.FindOrNullOneByConditionAsync<Status>(It.IsAny<Expression<Func<Status, bool>>>()))
                .Returns(Task.FromResult<Status>(new Status(){StatusId = 3}));
            repositoryMock.Setup(p => p.GetAsQueryable<Company>())
                .Returns(new TestAsyncEnumerable<Company>(
                    new List<Company>
                    {
                        new Company()
                        {
                            CompanyIndustryId = Guid.NewGuid(),
                            LanguageId = 2,
                            CountryId = Guid.NewGuid(),
                            CorporationId = Guid.NewGuid(),
                            StatusId = 3,
                            CompanyId = Guid.NewGuid(),
                            CompanyName = "HornAndHoves2",
                            IsExtended = false,
                            CreationDate = DateTime.Now,
                        }
                    })
                    .AsQueryable());
            repositoryMock.Setup(p => p.GetAsQueryable<ApplicationUser>())
                .Returns(new TestAsyncEnumerable<ApplicationUser>(
                    new List<ApplicationUser>
                    {
                        new ApplicationUser()
                        {
                            Id = Guid.NewGuid(),
                            FullName = "TestUser",
                            CreationDate = DateTime.Now,

                            NormalizedEmail = "HornAndHoves2@heedbook.com".ToUpper()
                        }
                    })
                    .AsQueryable());
            repositoryMock.Setup(p => p.Create<Company>(It.IsAny<Company>())).Verifiable();
            repositoryMock.Setup(p => p.GetAsQueryable<ApplicationRole>())
                .Returns(new TestAsyncEnumerable<ApplicationRole>(
                    new List<ApplicationRole>
                    {
                        new ApplicationRole()
                        {
                            Name = "Test"
                        }
                    })
                    .AsQueryable());
            //Act
            await accountService.RegisterNewCompanyAndUser(TestData.GetUserRegister());

            //Assert
            Assert.IsTrue(true);
        }
       
        [Test]
        public void GenerateTokenPostTest()
        {
            //Arrange
            repositoryMock.Setup(p => p.GetWithIncludeOne<ApplicationUser>(
                    It.IsAny<Expression<Func<ApplicationUser, bool>>>(), 
                    It.IsAny<Expression<Func<ApplicationUser, object>>>()))
                .Returns(new ApplicationUser()
                    {
                        Id = Guid.NewGuid(),
                        FullName = "TestUser",
                        CreationDate = DateTime.Now,
                        StatusId = 3,
                        NormalizedEmail = "HornAndHoves@heedbook.com".ToUpper()
                    });
            repositoryMock.Setup(p => p.FindOrNullOneByConditionAsync<Status>(It.IsAny<Expression<Func<Status, bool>>>()))
                .Returns(Task.FromResult<Status>(new Status(){StatusId = 3}));
            repositoryMock.Setup(p => p.GetAsQueryable<ApplicationUser>())
                .Returns(new List<ApplicationUser>
                {
                    new ApplicationUser()
                    {
                        Id = Guid.NewGuid(),
                        FullName = "TestUser",
                        CreationDate = DateTime.Now,
                        StatusId = 3,
                        NormalizedEmail = "HornAndHoves@heedbook.com".ToUpper(),
                        PasswordHash = "PasswordHash"
                    }
                }.AsQueryable());
            moqILoginService.Setup(p => p.CheckUserLogin(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);
            moqILoginService.Setup(p => p.CreateTokenForUser(It.IsAny<ApplicationUser>()))
                .Returns("token");

            //Act
            var result = accountService.GenerateToken(TestData.GetAccountAuthorization());
            
            //Assert           
            Assert.IsFalse(result is null);
        }
        
        [Test]
        public async Task ChangePasswordPostTest()
        {
            //Arrange
            var dictionary = new Dictionary<string, string>(){{"applicationUserId", $"{Guid.NewGuid()}"}};
            moqILoginService.Setup(p => p.GetDataFromToken(It.IsAny<string>(), out dictionary, It.IsAny<string>()))
                .Returns(true);
            moqILoginService.Setup(p => p.GeneratePasswordHash(It.IsAny<string>()))
                .Returns("passwordHash");
            repositoryMock.Setup(p => p.GetWithIncludeOne<ApplicationUser>(
                    It.IsAny<Expression<Func<ApplicationUser, bool>>>(), 
                    It.IsAny<Expression<Func<ApplicationUser, object>>>()))
                .Returns(new ApplicationUser()
                    {
                        Id = Guid.NewGuid(),
                        FullName = "TestUser",
                        CreationDate = DateTime.Now,
                        StatusId = 3,
                        NormalizedEmail = "HornAndHoves@heedbook.com".ToUpper()
                    });
            repositoryMock.Setup(p => p.SaveAsync()).Returns(Task.FromResult(0));

            //Act
            var result = await accountService.ChangePassword(TestData.GetAccountAuthorization());

            //Assert
            Assert.IsTrue(result == "Password changed");
        }
        [Test]
        public async Task ValidateTokenPostTest()
        {
            //Arrange
            Dictionary<string, string> claims;
            moqILoginService.Setup(p => p.GetDataFromToken(It.IsAny<string>(), out claims, It.IsAny<string>()))
                .Returns(true);

            //Act
            var result = await accountService.ValidateToken($"token");

            //Assert
            Assert.IsTrue(result.ContainsKey("status"));
        }
        [Test]
        public async Task UserChangePasswordOnDefaultAsyncPostTest()
        {
            //Arrange
            moqILoginService.Setup(p => p.GetCurrentUserId()).Verifiable();
            repositoryMock.Setup(p => p.GetWithIncludeOne<ApplicationUser>(
                    It.IsAny<Expression<Func<ApplicationUser, bool>>>(), 
                    It.IsAny<Expression<Func<ApplicationUser, object>>>()))
                .Returns(new ApplicationUser()
                    {
                        Id = Guid.NewGuid(),
                        FullName = "TestUser",
                        CreationDate = DateTime.Now,
                        StatusId = 3,
                        NormalizedEmail = "test@heedbook.com".ToUpper()
                    });
            repositoryMock.Setup(p => p.SaveAsync()).Returns(Task.FromResult(0));

            //Act
            var result = await accountService.ChangePasswordOnDefault($"test@heedbook.com");

            //Assert
            Assert.IsTrue(result == "Password changed");
        }
        [Test]
        public async Task RemoveDeleteTest()
        {
            //Arrange
            moqILoginService.Setup(p => p.GetCurrentUserId()).Verifiable();
            var testCompoanyId = Guid.NewGuid();
            repositoryMock.Setup(p => p.GetAsQueryable<ApplicationUser>())
                .Returns(new List<ApplicationUser>()
                    {
                        new ApplicationUser()
                        {
                            Id = Guid.NewGuid(),
                            FullName = "TestUser",
                            CreationDate = DateTime.Now,
                            CompanyId = testCompoanyId,
                            NormalizedEmail = "test@heedbook.com".ToUpper(),
                            Email = "test@heedbook.com"
                        }
                    }
                    .AsQueryable());
            repositoryMock.Setup(p => p.GetAsQueryable<Company>())
                .Returns(new List<Company>()
                    {
                        new Company()
                        {
                            CompanyId = testCompoanyId,
                            IsExtended = false,
                            CreationDate = DateTime.Now
                        }
                    }
                    .AsQueryable());
            repositoryMock.Setup(p => p.GetWithInclude<ApplicationUser>(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<Expression<Func<ApplicationUser, object>>>()))
                .Returns(new List<ApplicationUser>(){new ApplicationUser()
                    {
                        Id = Guid.NewGuid(),
                        FullName = "TestUser2",
                        CreationDate = DateTime.Now,
                        CompanyId = testCompoanyId,
                        NormalizedEmail = "test2@heedbook.com".ToUpper(),
                        UserRoles = new List<ApplicationUserRole>(){new ApplicationUserRole(){}}
                    }});
            var testTarifId = Guid.NewGuid();
            repositoryMock.Setup(p => p.GetAsQueryable<Tariff>())
                .Returns(new List<Tariff>()
                    {
                        new Tariff()
                        {
                            TariffId = testTarifId,
                            CompanyId = testCompoanyId
                        }
                    }
                    .AsQueryable());
            repositoryMock.Setup(p => p.GetAsQueryable<Transaction>())
                .Returns(new TestAsyncEnumerable<Transaction>(
                    new List<Transaction>()
                    {
                        new Transaction()
                        {
                            TariffId = testTarifId
                        }
                    }));
            repositoryMock.Setup(p => p.GetAsQueryable<Content>())
                .Returns(new List<Content>()
                    {
                        new Content()
                        {
                            ContentId = Guid.NewGuid(),
                            CompanyId = testCompoanyId,
                            Duration = 20,
                            IsTemplate = false,
                        }
                    }
                    .AsQueryable());
            repositoryMock.Setup(p => p.GetWithInclude<Campaign>(It.IsAny<Expression<Func<Campaign, bool>>>(), It.IsAny<Expression<Func<Campaign, object>>>()))
                .Returns(new List<Campaign>()
                    {
                        new Campaign()
                        {
                            CampaignId = Guid.NewGuid(),
                            CompanyId = testCompoanyId,
                            IsSplash = false,
                            GenderId = 3,
                            CampaignContents = new List<CampaignContent>()
                            {
                                new CampaignContent(){}
                            }
                        }
                    }
                    .AsQueryable());
            repositoryMock.Setup(p => p.GetAsQueryable<PhraseCompany>())
                .Returns(new List<PhraseCompany>()
                    {
                        new PhraseCompany()
                        {
                            PhraseCompanyId = Guid.NewGuid(),
                            CompanyId = testCompoanyId
                        }
                    }
                    .AsQueryable());
            repositoryMock.Setup(p => p.GetAsQueryable<WorkingTime>())
                .Returns(new List<WorkingTime>()
                    {
                        new WorkingTime()
                        {
                            Day = 1,
                            CompanyId = testCompoanyId
                        }
                    }
                    .AsQueryable());
            repositoryMock.Setup(p => p.GetAsQueryable<SalesStagePhrase>())
                .Returns(new List<SalesStagePhrase>()
                    {
                        new SalesStagePhrase()
                        {
                            SalesStagePhraseId = Guid.NewGuid(),
                            PhraseId = Guid.NewGuid(),
                            CompanyId = testCompoanyId,
                            SalesStageId = Guid.NewGuid()
                        }
                    }
                    .AsQueryable());
            repositoryMock.Setup(p => p.Delete<PhraseCompany>(It.IsAny<List<PhraseCompany>>()));
            repositoryMock.Setup(p => p.Delete<CampaignContent>(It.IsAny<List<CampaignContent>>()));
            repositoryMock.Setup(p => p.Delete<Campaign>(It.IsAny<List<Campaign>>()));
            repositoryMock.Setup(p => p.Delete<Content>(It.IsAny<List<Content>>()));
            repositoryMock.Setup(p => p.Delete<ApplicationUserRole>(It.IsAny<List<ApplicationUserRole>>()));
            repositoryMock.Setup(p => p.Delete<Transaction>(It.IsAny<List<Transaction>>()));
            repositoryMock.Setup(p => p.Delete<ApplicationUser>(It.IsAny<List<ApplicationUser>>()));
            repositoryMock.Setup(p => p.Delete<Tariff>(It.IsAny<Tariff>()));
            repositoryMock.Setup(p => p.Delete<WorkingTime>(It.IsAny<List<WorkingTime>>()));
            repositoryMock.Setup(p => p.Delete<SalesStagePhrase>(It.IsAny<List<SalesStagePhrase>>()));
            repositoryMock.Setup(p => p.Delete<Company>(It.IsAny<Company>()));
            repositoryMock.Setup(p => p.Save());

            //Act
             var result = await accountService.DeleteCompany("test@heedbook.com");

            //Assert
            Assert.IsTrue(result == "Removed");
        }
        [Test]
        public void DeleteUserDeleteTest()
        {
            //Arrange
            var testCompoanyId = Guid.NewGuid();
            repositoryMock.Setup(p => p.GetAsQueryable<ApplicationUser>())
                .Returns(new List<ApplicationUser>()
                    {
                        new ApplicationUser()
                        {
                            Id = Guid.NewGuid(),
                            FullName = "TestUser",
                            CreationDate = DateTime.Now,
                            CompanyId = testCompoanyId,
                            NormalizedEmail = "test@heedbook.com".ToUpper(),
                            Email = "test@heedbook.com",
                            UserRoles = new List<ApplicationUserRole>(){new ApplicationUserRole(){}}
                        }
                    }
                    .AsQueryable());
            repositoryMock.Setup(p => p.Delete<ApplicationUser>(It.IsAny<List<ApplicationUser>>()));
            repositoryMock.Setup(p => p.Delete<ApplicationUserRole>(It.IsAny<List<ApplicationUserRole>>()));
            repositoryMock.Setup(p => p.Save());

            //Act
            accountService.DeleteUser("test@heedbook.com");

            //Assert
            Assert.IsTrue(true);
        }
        [Test]
        public void AddPhrasesFromExcelGetTest()
        {
            //Arrange
            moqILoginService.Setup(p => p.GetCurrentUserId()).Verifiable();
            spreadSheetDocumentUtils.Setup(p => p.AddComanyPhrases(It.IsAny<string>()));

            //Act
            accountService.AddPhrasesFromExcel("file.xlsx");

            //Assert
            Assert.IsTrue(true);
        }
        [Test]
        public async Task EmptyTokenPostTest()
        {
            //Arrange
            moqILoginService.Setup(p => p.CreateTokenEmpty())
                .Returns("emptyToken");

            //Act
            var result = await accountService.CreateEmptyToken();

            //Assert
            Assert.IsTrue(result == "emptyToken");
        }
    }
}