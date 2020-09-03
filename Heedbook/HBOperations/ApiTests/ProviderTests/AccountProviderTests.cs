using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using UserOperations.Controllers;
using UserOperations.AccountModels;
using HBData.Models.AccountViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System;
using HBData.Models;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using System.Threading;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace ApiTests
{
    public class AccountProviderTests : ApiServiceTest
    {
        [SetUp]
        public new void Setup()
        {
            base.Setup();
        }
      
        [Test]
        public void GetStatusIdTest()
        {
            //Arrange
            //var status = new Status(){StatusId = 3};
            //repositoryMock.Setup(p => p.FindOrNullOneByConditionAsync<Status>(It.IsAny<Expression<Func<Status, bool>>>()))
            //    .Returns(Task.FromResult<Status>(status));

            //var accountProvider = new AccountProvider(moqILoginService.Object, repositoryMock.Object);            

            ////Act
            //var statusId = accountProvider.GetStatusId("Action");

            ////Assert
            //Assert.AreEqual(statusId, 3);
        }
        [Test]
        public async Task CompanyExistTest()
        {
            ////Arrange
            //var companys = new TestAsyncEnumerable<Company>(new List<Company>()
            //    {
            //        new Company(){CompanyName = "HornsAndHoves"}, 
            //        new Company(){CompanyName = "ManShoes"}
            //    }
            //).AsQueryable();
            
            //repositoryMock.Setup(p => p.GetAsQueryable<Company>()).Returns(companys);            
            //var accountProvider = new AccountProvider(moqILoginService.Object, repositoryMock.Object);         

            ////Act
            //var companyExist = await accountProvider.CompanyExist("HornsAndHoves");
            
            ////Assert
            //Assert.IsTrue(companyExist);
        }
        [Test]
        public async Task EmailExistTest()
        {
            //Arrange
            //var users = new TestAsyncEnumerable<ApplicationUser>(new List<ApplicationUser>()
            //    {
            //        new ApplicationUser(){NormalizedEmail = "IVANOV@HEEDBOOK.COM"}, 
            //        new ApplicationUser(){NormalizedEmail = "PETROV@HEEDBOOK.COM"}
            //    }
            //).AsQueryable();
            
            //repositoryMock.Setup(p => p.GetAsQueryable<ApplicationUser>()).Returns(users);            
            //var accountProvider = new AccountProvider(moqILoginService.Object, repositoryMock.Object);         

            ////Act
            //var companyExist = await accountProvider.EmailExist("Ivanov@heedbook.com");
            
            ////Assert
            //Assert.IsTrue(companyExist);
        }
        [Test]
        public void AddNewCompanysInBaseTest()
        {
            ////Arrange
            //repositoryMock.Setup(p => p.Create<Company>(It.IsAny<Company>())).Verifiable();  
            //var status = new Status(){StatusId = 5};   
            //repositoryMock.Setup(p => p.FindOrNullOneByConditionAsync<Status>(It.IsAny<Expression<Func<Status, bool>>>()))
            //    .Returns(Task.FromResult<Status>(status));     
            //var accountProvider = new AccountProvider(moqILoginService.Object, repositoryMock.Object);

            ////Act
            //var company = accountProvider.AddNewCompanysInBase(TestData.GetUserRegister());

            ////Assert
            //Assert.AreEqual(5, company.StatusId);
            //Assert.AreEqual("HornAndHoves", company.CompanyName);
        }
        [Test]
        public async Task AddNewUserInBase()
        {
            ////Arrange
            //repositoryMock.Setup(p => p.Create<ApplicationUser>(It.IsAny<ApplicationUser>())).Verifiable();  
            //var status = new Status(){StatusId = 3};
            //repositoryMock.Setup(p => p.FindOrNullOneByConditionAsync<Status>(It.IsAny<Expression<Func<Status, bool>>>()))
            //    .Returns(Task.FromResult<Status>(status));

            //var accountProvider = new AccountProvider(moqILoginService.Object, repositoryMock.Object);

            ////Act
            //var user = await accountProvider.AddNewUserInBase(TestData.GetUserRegister(), Guid.NewGuid());

            ////Assert
            //Assert.AreEqual(TestData.GetUserRegister().Email, user.Email);
            //Assert.AreEqual("Hash", user.PasswordHash);
        }
        [Test]
        public async Task AddUserRoleInBaseTest()
        {
            ////Arrange
            //var roles = TestData.GetApplicationRoles().AsQueryable();
            //repositoryMock.Setup(p => p.GetAsQueryable<ApplicationRole>()).Returns(roles);
            //var userRoles = new List<ApplicationUserRole>();
            //repositoryMock.Setup(p => p.Create<ApplicationUserRole>(It.IsAny<ApplicationUserRole>()))
            //    .Callback((ApplicationUserRole r) => userRoles.Add(r));
            //var message = new UserRegister()
            //{
            //    Role = "Manager"
            //};

            //var accountProvider = new AccountProvider(moqILoginService.Object, repositoryMock.Object);

            ////Act
            //await accountProvider.AddUserRoleInBase(message, new ApplicationUser(){Id = Guid.NewGuid()});

            ////Assert
            //Assert.IsTrue(userRoles.Any());
        }
        [Test]
        public async Task GetTariffsTest()
        {
            ////Arrange
            //var tariffs = new List<Tariff>()
            //{
            //    new Tariff{CompanyId = new Guid("55b74216-7871-4f5b-b21f-9bcf5177a120")},
            //    new Tariff{CompanyId = new Guid("55b74216-7871-4f5b-b21f-9bcf5177a120")}
            //}.AsEnumerable();
            //repositoryMock.Setup(p => p.FindByConditionAsync<Tariff>(It.IsAny<Expression<Func<Tariff, bool>>>()))
            //   .Returns(Task.FromResult<IEnumerable<Tariff>>(tariffs));
            //var accountProvider = new AccountProvider(moqILoginService.Object, base.repositoryMock.Object);

            ////Act
            //var count = await accountProvider.GetTariffsAsync(Guid.Parse("55b74216-7871-4f5b-b21f-9bcf5177a120"));

            ////Assert
            //Assert.AreEqual(2, count);
        }
        [Test]
        public async Task CreateCompanyTariffAndtransaction()
        {
            ////Arrange
            //var status = new Status(){};
            //repositoryMock.Setup(p => p.FindOrNullOneByConditionAsync<Status>(It.IsAny<Expression<Func<Status, bool>>>())).Returns(Task.FromResult<Status>(status));
            //var tariffs = new List<Tariff>();
            //repositoryMock.Setup(p => p.Create<Tariff>(It.IsAny<Tariff>()))
            //    .Callback((Tariff t) => tariffs.Add(t));
            //var transactions = new List<Transaction>();
            //repositoryMock.Setup(p => p.Create<Transaction>(It.IsAny<Transaction>()))
            //    .Callback((Transaction t) => transactions.Add(t));
            //var accountProvider = new AccountProvider(moqILoginService.Object, repositoryMock.Object);

            ////Act
            //await accountProvider.CreateCompanyTariffAndTransaction(new Company(){CompanyId = Guid.NewGuid()});

            ////Assert
            //Assert.IsTrue(tariffs.Any());
            //Assert.IsTrue(transactions.Any());
        }
        [Test]
        public async Task AddWorkerTypeTest()
        {
            ////Arrange
            //var workerTypes = new List<WorkerType>();
            //repositoryMock.Setup(p => p.Create<WorkerType>(It.IsAny<WorkerType>()))
            //    .Callback((WorkerType wt) => workerTypes.Add(wt));

            //var accountProvider = new AccountProvider(moqILoginService.Object, repositoryMock.Object);

            ////Act
            //await accountProvider.AddWorkerType(new Company(){CompanyId = Guid.NewGuid()});

            ////Assert
            //Assert.IsTrue(workerTypes.Any());
        }
        [Test]
        public async Task AddContentAndCampaignTest()
        {
            ////Arrange
            //var campaignContents = new List<CampaignContent>();
            //repositoryMock.Setup(p => p.Create<Content>(It.IsAny<Content>())).Verifiable();
            //var content = new Content(){};
            //repositoryMock.Setup(p => p.FindOrNullOneByConditionAsync<Content>(It.IsAny<Expression<Func<Content, bool>>>()))
            //    .Returns(Task.FromResult<Content>(content));
            //var status = new Status(){StatusId = 3};   
            //repositoryMock.Setup(p => p.FindOrNullOneByConditionAsync<Status>(It.IsAny<Expression<Func<Status, bool>>>()))
            //    .Returns(Task.FromResult<Status>(status));   
            //repositoryMock.Setup(p => p.Create<CampaignContent>(It.IsAny<CampaignContent>()))
            //    .Callback((CampaignContent cc) => campaignContents.Add(cc));

            //var accountProviderMock = new AccountProvider(moqILoginService.Object, repositoryMock.Object);

            ////Act
            //await accountProviderMock.AddContentAndCampaign(new Company(){CompanyId = Guid.NewGuid()});

            ////Assert
            //Assert.IsTrue(campaignContents.Any());
        }
        [Test]
        public void GetUserIncludeCompanyTest()
        {
            ////Arrange
            //var user = new ApplicationUser(){NormalizedEmail = "IVANOVIVAN@HEEDBOOK.COM"};
            //repositoryMock.Setup(p => p.GetWithIncludeOne<ApplicationUser>(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<Expression<Func<ApplicationUser, object>>[]>()))
            //    .Returns(user);

            //var accountProviderMock = new AccountProvider(moqILoginService.Object, repositoryMock.Object);

            ////Act
            //var newUser = accountProviderMock.GetUserIncludeCompany("IvanovIvan@heedbook.com");

            ////Assert
            //Assert.AreEqual(user.NormalizedEmail, newUser.NormalizedEmail);
        }
        [Test]
        public void RemoveAccount()
        {
            // var users = TestData.GetUsers();
            // var companys = TestData.GetCompanys();
            // var user = new ApplicationUser(){NormalizedEmail = "IVANOVIVAN@HEEDBOOK.COM"};
            // var tariffs = TestData.GetTariffs();
            // var transactions = TestData.GetTransactions();
            // var workerTypes = TestData.GetWorkerTypes();
            // var campaigns = new List<Campaign>
            // {
            //     new Campaign
            //     {
            //         CompanyId = new Guid("14f335c2-c64f-42cc-8ca3-dadd6a623ae2"),
            //         CampaignContents = new List<CampaignContent>
            //         {
            //             new CampaignContent{},
            //             new CampaignContent{}
            //         }
            //     }
            // };
            // var contents = new TestAsyncEnumerable<Content>(
            //     new List<Content>
            //     {
            //         new Content
            //         {
            //             CompanyId = new Guid("14f335c2-c64f-42cc-8ca3-dadd6a623ae2")
            //         }
            //     }) .AsQueryable();
            // var phraseCompanys = TestData.GetPhraseCompanies();
            // var passwordHistory = TestData.GetPasswordHistorys();

            // //Mock
            // repositoryMock.Setup(p => p.GetAsQueryable<ApplicationUser>()).Returns(users);
            
            // repositoryMock.Setup(p => p.GetAsQueryable<Company>()).Returns(companys);            
            // repositoryMock.Setup(p => p.GetWithInclude<ApplicationUser>(
            //         It.IsAny<Expression<Func<ApplicationUser, bool>>>(), 
            //         It.IsAny<Expression<Func<ApplicationUser, object>>[]>()))
            //     .Returns(users);            
            // repositoryMock.Setup(p => p.GetAsQueryable<Tariff>()).Returns(tariffs);            
            // repositoryMock.Setup(p => p.GetAsQueryable<Transaction>()).Returns(transactions);            
            // repositoryMock.Setup(p => p.GetAsQueryable<WorkerType>()).Returns(workerTypes);            
            // repositoryMock.Setup(p => p.GetAsQueryable<Content>()).Returns(contents);            
            // repositoryMock.Setup(p => p.GetWithInclude<Campaign>(
            //         It.IsAny<Expression<Func<Campaign, bool>>>(), 
            //         It.IsAny<Expression<Func<Campaign, object>>[]>()))
            //     .Returns(campaigns);            
            // repositoryMock.Setup(p => p.GetAsQueryable<PhraseCompany>()).Returns(phraseCompanys);            
            // repositoryMock.Setup(p => p.GetAsQueryable<PasswordHistory>()).Returns(passwordHistory);

            // //Mock delete methods
            // var basePasswordHistory = passwordHistory.ToList();
            // repositoryMock.Setup(p => p.Delete<PasswordHistory>(It.IsAny<IEnumerable<PasswordHistory>>()))
            //     .Callback((IEnumerable<PasswordHistory> passwordhistory) => basePasswordHistory.RemoveAll(p => passwordHistory.Contains(p)));

            // var basePhraseCompany = phraseCompanys.ToList();
            // repositoryMock.Setup(p => p.Delete<PhraseCompany>(It.IsAny<IEnumerable<PhraseCompany>>()))
            //     .Callback((IEnumerable<PhraseCompany> phrases) => basePhraseCompany.RemoveAll(p => phrases.Contains(p)));
            
            // var baseCampaignContents = campaigns.FirstOrDefault().CampaignContents.ToList();
            // repositoryMock.Setup(p => p.Delete<CampaignContent>(It.IsAny<IEnumerable<CampaignContent>>()))
            //     .Callback((IEnumerable<CampaignContent> campaignContents) => baseCampaignContents.RemoveAll(p => campaignContents.Contains(p)));

            // var baseCampaigns = campaigns.ToList();
            // repositoryMock.Setup(p => p.Delete<Campaign>(It.IsAny<IEnumerable<Campaign>>()))
            //     .Callback((IEnumerable<Campaign> campaign) => baseCampaigns.RemoveAll(p => campaign.Contains(p)));

            // var baseContents = contents.ToList();
            // repositoryMock.Setup(p => p.Delete<Content>(It.IsAny<IEnumerable<Content>>()))
            //     .Callback((IEnumerable<Content> contentList) => baseContents.RemoveAll(p => contentList.Contains(p)));

            // var baseWorkerType = workerTypes.ToList();
            // repositoryMock.Setup(p => p.Delete<WorkerType>(It.IsAny<IEnumerable<WorkerType>>()))
            //     .Callback((IEnumerable<WorkerType> workerTypesList) => baseWorkerType.RemoveAll(p => workerTypesList.Contains(p)));

            // var baseApplicationUserRoles = users.FirstOrDefault().UserRoles.ToList();
            // repositoryMock.Setup(p => p.Delete<ApplicationUserRole>(It.IsAny<IEnumerable<ApplicationUserRole>>()))
            //     .Callback((IEnumerable<ApplicationUserRole> userRoles) => baseApplicationUserRoles.RemoveAll(p => userRoles.Contains(p)));

            // var baseTransactions = transactions.ToList();
            // repositoryMock.Setup(p => p.Delete<Transaction>(It.IsAny<IEnumerable<Transaction>>()))
            //     .Callback((IEnumerable<Transaction> transactionsList) => baseTransactions.RemoveAll(p => transactionsList.Contains(p)));  

            // var baseUsers = users.ToList();
            // repositoryMock.Setup(p => p.Delete<ApplicationUser>(It.IsAny<IEnumerable<ApplicationUser>>()))
            //     .Callback((IEnumerable<ApplicationUser> usersList) => baseUsers.RemoveAll(p => users.Contains(p)));

            // var baseTariffs = tariffs.ToList();
            // repositoryMock.Setup(p => p.Delete<Tariff>(It.IsAny<Tariff>()))
            //     .Callback((Tariff tariff) => baseTariffs.Remove(tariff));

            // var baseCompanys = companys.ToList();

            // repositoryMock.Setup(p => p.Delete<Company>(It.IsAny<Company>()))
            //     .Callback((Company company) => baseCompanys.Remove(company));

            // repositoryMock.Setup(p => p.Save()).Verifiable();

            // //Act
            // //var accountProvider = new AccountProvider(moqILoginService.Object, repositoryMock.Object);
            // //Task t = accountProvider.RemoveAccountWithSave(users.FirstOrDefault().Email);
            // //t.Start();
            // //t.Wait();

            // ////Assert
            // //Assert.AreEqual(basePasswordHistory.Count, 0);
            // //Assert.AreEqual(basePhraseCompany.Count, 0);
            // //Assert.AreEqual(baseCampaignContents.Count, 0);
            // //Assert.AreEqual(baseCampaigns.Count, 0);
            // //Assert.AreEqual(baseContents.Count, 0);
            // //Assert.AreEqual(baseWorkerType.Count, 0);
            // //Assert.AreEqual(baseApplicationUserRoles.Count, 0);
            // //Assert.AreEqual(baseTransactions.Count, 0);
            // //Assert.AreEqual(baseUsers.Count, 0);
            // //Assert.AreEqual(baseTariffs.Count, 0);
            // //Assert.AreEqual(baseCompanys.Count, 0);
        }
    }
}