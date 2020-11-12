using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using UserOperations.Controllers;
using UserOperations.AccountModels;
using HBData.Models.AccountViewModels;
using Microsoft.AspNetCore.Mvc;
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
    public class AnalyticOfficeProviderTests : ApiServiceTest
    {
        protected Mock<IGenericRepository> genericRepository;
        [SetUp]
        public new void Setup()
        {
            base.Setup();
            genericRepository = new Mock<IGenericRepository>();
        }
        [Test]
        public void GetSessionsInfoTest()
        {
            ////Arrange
            //var user = new ApplicationUser()
            //{
            //    Id = new Guid("55b74216-7871-4f5b-b21f-9bcf5177a120"),
            //    CompanyId = new Guid("55b74216-7871-4f5b-b21f-9bcf5177a121"),
            //    WorkerTypeId = new Guid("55b74216-7871-4f5b-b21f-9bcf5177a122")
            //};
            //var mockSessions = new List<Session>
            //{
            //    new Session
            //    {
            //        ApplicationUser = user,
            //        StatusId = 7,
            //        ApplicationUserId = user.Id,
            //        BegTime = new DateTime(2019, 11, 10, 05, 00, 00),
            //        EndTime = new DateTime(2019, 11, 10, 06, 00, 00)                    
            //    },
            //};
            //genericRepository.Setup(p => p.GetWithInclude<Session>(
            //        It.IsAny<Expression<Func<Session, bool>>>(), 
            //        It.IsAny<Expression<Func<Session, object>>>()))
            //    .Returns(mockSessions);
            

            //var analyticOfficeProvider = new AnalyticOfficeProvider(moqILoginService.Object, genericRepository.Object);
            ////Act
            //var sessions = analyticOfficeProvider.GetSessionsInfo(
            //        new DateTime(2019, 11, 10, 01, 00, 00),
            //        new DateTime(2019, 11, 10, 03, 00, 00),
            //        new List<Guid>{new Guid("55b74216-7871-4f5b-b21f-9bcf5177a121")},
            //        new List<Guid>{new Guid("55b74216-7871-4f5b-b21f-9bcf5177a120")},
            //        new List<Guid>{new Guid("55b74216-7871-4f5b-b21f-9bcf5177a122")}
            //);

            ////Assert
            //Assert.AreEqual(sessions.Count, 1);
        }   
        [Test]
        public void GetDialoguesInfoTest()
        {
            ////Arrange
            //var user = new ApplicationUser()
            //{
            //    Id = new Guid("55b74216-7871-4f5b-b21f-9bcf5177a120"),
            //    CompanyId = new Guid("55b74216-7871-4f5b-b21f-9bcf5177a121"),
            //    WorkerTypeId = new Guid("55b74216-7871-4f5b-b21f-9bcf5177a122"),
            //    FullName = "Барбарис"
            //};
            //var mockDialogues = new List<Dialogue>
            //{
            //    new Dialogue
            //    {
            //        DialogueId = new Guid("55b74216-1111-4f5b-b21f-9bcf5177a121"),
            //        ApplicationUser = user,
            //        ApplicationUserId = user.Id,
            //        BegTime = new DateTime(2019, 11, 10, 05, 00, 00),
            //        EndTime = new DateTime(2019, 11, 10, 06, 00, 00),
            //        StatusId = 3,
            //        InStatistic = true,
            //        DialogueClientSatisfaction = new List<DialogueClientSatisfaction>
            //        {
            //            new DialogueClientSatisfaction
            //            {
            //                MeetingExpectationsTotal = 0.8d
            //            }
            //        }
            //    },
            //    new Dialogue
            //    {
            //        DialogueId = new Guid("55b74216-2222-4f5b-b21f-9bcf5177a121"),
            //        ApplicationUser = user,
            //        ApplicationUserId = user.Id,
            //        BegTime = new DateTime(2019, 11, 10, 06, 00, 00),
            //        EndTime = new DateTime(2019, 11, 10, 07, 00, 00),
            //        StatusId = 3,
            //        InStatistic = true,
            //        DialogueClientSatisfaction = new List<DialogueClientSatisfaction>
            //        {
            //            new DialogueClientSatisfaction
            //            {
            //                MeetingExpectationsTotal = 0.8d
            //            }
            //        }
            //    },
            //    new Dialogue
            //    {
            //        DialogueId = new Guid("55b74216-3333-4f5b-b21f-9bcf5177a121"),
            //        ApplicationUser = user,
            //        ApplicationUserId = user.Id,
            //        BegTime = new DateTime(2019, 11, 10, 07, 00, 00),
            //        EndTime = new DateTime(2019, 11, 10, 08, 00, 00),
            //        StatusId = 3,
            //        InStatistic = true,
            //        DialogueClientSatisfaction = new List<DialogueClientSatisfaction>
            //        {
            //            new DialogueClientSatisfaction
            //            {
            //                MeetingExpectationsTotal = 0.8d
            //            }
            //        }
            //    }
            //};

            //genericRepository.Setup(p => p.GetWithInclude<Dialogue>(
            //        It.IsAny<Expression<Func<Dialogue, bool>>>(), 
            //        It.IsAny<Expression<Func<Dialogue, object>>>()))
            //    .Returns(mockDialogues);
            

            //var analyticOfficeProvider = new AnalyticOfficeProvider(moqILoginService.Object, genericRepository.Object);
            ////Act
            //var dialogues = analyticOfficeProvider.GetDialoguesInfo(
            //        new DateTime(2019, 11, 10, 01, 00, 00),
            //        new DateTime(2019, 11, 10, 10, 00, 00),
            //        new List<Guid>{new Guid("55b74216-7871-4f5b-b21f-9bcf5177a121")},
            //        new List<Guid>{new Guid("55b74216-7871-4f5b-b21f-9bcf5177a120")},
            //        new List<Guid>{new Guid("55b74216-7871-4f5b-b21f-9bcf5177a122")}
            //);

            ////Assert
            //System.Console.WriteLine($"dialogue.count: {dialogues.Count}");
            //Assert.AreEqual(dialogues.Count, 3);
        }     
    }    
}