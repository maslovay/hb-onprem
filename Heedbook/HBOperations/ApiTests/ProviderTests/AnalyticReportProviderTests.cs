using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using UserOperations.Controllers;
using UserOperations.AccountModels;
using HBData.Models.AccountViewModels;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Providers;
using System.Linq.Expressions;
using System;
using HBData;
using HBData.Models;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using System.Threading;
using Microsoft.EntityFrameworkCore.Query.Internal;
using UserOperations.Models.AnalyticModels;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using HBData.Repository;

namespace ApiTests
{
    public class AnalyticReportroviderTests : ApiServiceTest
    {
        protected Mock<IGenericRepository> genericRepository;
        [SetUp]
        public new void Setup()
        {
            base.Setup();
            genericRepository = new Mock<IGenericRepository>();
        }
        [Test]
        public void GetSessionsTest1()
        {
            ////Arrange
            //var sessions = TestData.GetQueryableSessions();
            //genericRepository.Setup(p => p.GetAsQueryable<Session>()).Returns(sessions);
            //var analyticOfficeProvider = new AnalyticReportProvider(genericRepository.Object);

            ////Act
            //var sessionInfos = analyticOfficeProvider.GetSessions(
            //    new List<Guid>{Guid.Parse("154d371a-acbc-48a0-9731-ab1d9a1f1710")},
            //    new List<Guid>{Guid.Parse("b54d371a-acbc-48a0-9731-ab1d9a1f171b")},
            //    new List<Guid>{Guid.Parse("b54d371a-acbc-48a0-9731-ab1d9a1f1712")}
            //);

            ////Assert
            //Assert.AreEqual(sessionInfos.Count, 1);

        }
        [Test]
        public void GetSessionsTest2()
        {
            ////Arrange 
            //var sessions = TestData.GetQueryableSessions();
            //genericRepository.Setup(p => p.GetAsQueryable<Session>()).Returns(sessions);
            //var analyticOfficeProvider = new AnalyticReportProvider(genericRepository.Object);

            ////Act
            //var sessionInfos = analyticOfficeProvider.GetSessions(
            //    new DateTime(2019, 11, 26, 11, 00, 00),
            //    new DateTime(2019, 11, 26, 15, 00, 00),
            //    Guid.Parse("b54d371a-2222-48a0-9731-ab1d9a1f171a"),
            //    new List<Guid>{Guid.Parse("154d371a-acbc-48a0-9731-ab1d9a1f1710")},
            //    new List<Guid>{Guid.Parse("b54d371a-acbc-48a0-9731-ab1d9a1f171a")},
            //    new List<Guid>{Guid.Parse("b54d371a-acbc-48a0-9731-ab1d9a1f1711")}
            //);
            //System.Console.WriteLine($"sessionsInfoscount: {sessionInfos.Count}");

            ////Assert
            //Assert.AreEqual(sessionInfos.Count, 1);
        }
        [Test]
        public void GetEmployeeRoleIdTest()
        {
            ////Arrange
            //var roles = new List<ApplicationRole>
            //{
            //    new ApplicationRole
            //    {
            //        Id = Guid.Parse("9f18464e-4418-4575-880f-dfb6cccc2f7e"),
            //        Name = "Employee"
            //    }
            //}.AsQueryable();
            //genericRepository.Setup(p => p.GetAsQueryable<ApplicationRole>())
            //    .Returns(roles);
            //var analyticOfficeProvider = new AnalyticReportProvider(genericRepository.Object);
            ////Act
            
            //var id = analyticOfficeProvider.GetEmployeeRoleId();

            ////Assert
            //Assert.AreEqual(id, Guid.Parse("9f18464e-4418-4575-880f-dfb6cccc2f7e"));
        }
        [Test]
        public void GetDialoguesTest()
        {
            ////Arrange            
            //var dialogues = TestData.GetDialoguesIncluded();
            //genericRepository.Setup(p => p.GetAsQueryable<Dialogue>()).Returns(dialogues);
            //var analyticOfficeProvider = new AnalyticReportProvider(genericRepository.Object);

            ////Act
            //var dialogueInfos = analyticOfficeProvider.GetDialogues(
            //    new DateTime(2019, 10, 04, 11, 00, 00),
            //    new DateTime(2019, 10, 04, 14, 00, 00),
            //    new List<Guid>{Guid.Parse("82560395-2cc3-46e8-bcef-c844f1048182")},
            //    new List<Guid>{Guid.Parse("8d5cd62c-2ea0-406e-8ec1-a544d048a9d0")},
            //    new List<Guid>{Guid.Parse("1d5cd11c-2ea0-111e-8ec1-a544d048a9d0")}
            //);

            ////Assert
            //Assert.AreEqual(dialogueInfos.Count, 1);
        }
        [Test]
        public void GetDialoguesWithWorkerTypeTest()
        {
            ////Arrange            
            //var dialogues = TestData.GetDialoguesIncluded();
            //genericRepository.Setup(p => p.GetAsQueryable<Dialogue>()).Returns(dialogues);
            //var analyticOfficeProvider = new AnalyticReportProvider(genericRepository.Object);
            
            ////Act
            //var dialogueInfos = analyticOfficeProvider.GetDialoguesWithWorkerType(
            //    new DateTime(2019, 10, 04, 11, 00, 00),
            //    new DateTime(2019, 10, 04, 14, 00, 00),
            //    Guid.Parse("8f8947da-5f76-4c6e-2222-170290c87194"),
            //    new List<Guid>{Guid.Parse("82560395-2cc3-46e8-bcef-c844f1048182")},
            //    new List<Guid>{Guid.Parse("8d5cd62c-2ea0-406e-8ec1-a544d048a9d0")},
            //    new List<Guid>{Guid.Parse("1d5cd11c-2ea0-111e-8ec1-a544d048a9d0")}
            //);

            ////Assert
            //Assert.AreEqual(dialogueInfos.Count, 1);

        }
        [Test]
        public void GetApplicationUsersToAddTest()
        {
            ////Arrange
            //var users = TestData.GetUsersCompany1().AsQueryable();
            //genericRepository.Setup(p => p.GetAsQueryable<ApplicationUser>()).Returns(users);
            //var analyticOfficeProvider = new AnalyticReportProvider(genericRepository.Object);
            
            ////Act
            //var applicationUsers = analyticOfficeProvider.GetApplicationUsersToAdd(
            //    new DateTime(2019, 11, 24, 12, 00, 00),
            //    new List<Guid>{Guid.Parse("82560395-2cc3-46e8-bcef-c844f1048182")},
            //    new List<Guid>{Guid.Parse("8d5cd62c-2ea0-406e-8ec1-a544d048a9d0")},
            //    new List<Guid>{Guid.Parse("1d5cd11c-2ea0-111e-8ec1-a544d048a9d0")},
            //    new List<Guid>{},
            //    new Dictionary<string, string>{{"applicationUserId","e7323f63-b956-4507-9f18-b3a231932571"}},
            //    Guid.Parse("8f8947da-5f76-4c6e-2222-170290c87194")
            //);
            //System.Console.WriteLine($"applicationUserscount: {applicationUsers.Count}");
            ////Assert
            //Assert.AreEqual(applicationUsers.Count, 1);
        }
    }   
}