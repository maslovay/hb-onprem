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

namespace ApiTests
{
    public class DeviceControllerTests : ApiServiceTest
    {
        private DeviceService deviceService;
        [SetUp]
        public new void Setup()
        {
            base.Setup();
            deviceService = new DeviceService(
                moqILoginService.Object,
                requestFiltersMock.Object,
                repositoryMock.Object);
        }
        [Test]
        public async Task GenerateTokenPostTest()
        {
            //Arrange
            var deviceId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            repositoryMock.Setup(p => p.GetWithIncludeOne<Device>(It.IsAny<Expression<Func<Device, bool>>>(), It.IsAny<Expression<Func<Device, object>>[]>()))
                .Returns(
                    new Device
                    {
                        Code = "AAAAAA",
                        CompanyId = companyId,
                        Company = new Company
                        {
                            CompanyId = companyId
                        },
                        StatusId = 3
                    });
            repositoryMock.Setup(p => p.GetAsQueryable<Status>())
                .Returns(new TestAsyncEnumerable<Status>(new List<Status>
                {
                    new Status
                    {
                        StatusId = 3,
                        StatusName = "Active"
                    }
                }.AsQueryable()));
            moqILoginService.Setup(p => p.CreateTokenForDevice(It.IsAny<Device>()))
                .Returns("Token");

            //Act
            var result = await deviceService.GenerateToken("code");

            //Assert
            Assert.IsTrue(result == "Token");
        }
        [Test]
        public async Task GetAllUsersSessionsGetTest()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentDeviceId())
                .Returns(deviceId);
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
                                RoleId = roleId
                            }
                        },
                        StatusId = 3,
                        FullName = "Test Test Test",
                        Avatar = "avatar.jpg"
                    }
                }.AsQueryable()));
            repositoryMock.Setup(p => p.GetAsQueryable<Status>())
                .Returns(new TestAsyncEnumerable<Status>(new List<Status>
                {
                    new Status
                    {
                        StatusId = 3,
                        StatusName = "Active"
                    }
                }.AsQueryable()));
            repositoryMock.Setup(p => p.GetAsQueryable<Session>())
                .Returns(new TestAsyncEnumerable<Session>(new List<Session>
                {
                    new Session
                    {
                        ApplicationUserId = userId,
                        DeviceId = deviceId,
                        BegTime = DateTime.Now.AddMinutes(-3),
                        EndTime = DateTime.Now.AddMinutes(-1),
                        StatusId = 6
                    }
                }.AsQueryable()));
            
            //Act
            var result = await deviceService.GetAllUsersSessions(true);

            //Assert
            Assert.IsTrue(result.Count > 0);
        }
        [Test]
        public async Task GetAllGetTest()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Superuser");
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            var companyIds = new List<Guid>{companyId};
            requestFiltersMock.Setup(p => p.CheckRolesAndChangeCompaniesInFilter(ref companyIds, It.IsAny<List<Guid>>(), It.IsAny<string>(), It.IsAny<Guid>()));
            repositoryMock.Setup(p => p.GetAsQueryable<Device>())
                .Returns(new TestAsyncEnumerable<Device>(new List<Device>
                {
                    new Device
                    {
                        DeviceId = deviceId,
                        CompanyId = companyId,
                        StatusId = 3
                    }
                }.AsQueryable()));
            repositoryMock.Setup(p => p.GetAsQueryable<Status>())
                .Returns(new TestAsyncEnumerable<Status>(new List<Status>
                {
                    new Status
                    {
                        StatusId = 4,
                        StatusName = "Disabled"
                    }
                }.AsQueryable()));

            //Act
            var result = await deviceService.GetAll(
                new List<Guid>{companyId});

            //Assert
            Assert.IsTrue(result.Count > 0);
        }
        [Test]
        public async Task CreatePostTest()
        {
            //Arrange
            var deviceTypeId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Superuser");
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            moqILoginService.Setup(p => p.GetCurrentCorporationId())
                .Returns(corporationId);
            requestFiltersMock.Setup(p => p.IsCompanyBelongToUser(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<String>()))
                .Returns(true);
            repositoryMock.Setup(p => p.GetAsQueryable<Device>())
                .Returns(new TestAsyncEnumerable<Device>(new List<Device>
                {
                    new Device
                    {
                        DeviceId = Guid.NewGuid(),
                        CompanyId = companyId,
                        StatusId = 3
                    }
                }.AsQueryable()));
            repositoryMock.Setup(p => p.GetAsQueryable<Status>())
                .Returns(new TestAsyncEnumerable<Status>(new List<Status>
                {
                    new Status
                    {
                        StatusId = 4,
                        StatusName = "Active"
                    }
                }.AsQueryable()));
            var model = new PostDevice
            {
                Name = "TestDevice",
                Code = "AAAAAA",
                CompanyId = companyId,
                DeviceTypeId = deviceTypeId
            };
            repositoryMock.Setup(p => p.CreateAsync<CampaignContentAnswer>(It.IsAny<CampaignContentAnswer>()))
                .Returns(Task.FromResult(0));
            repositoryMock.Setup(p => p.SaveAsync())
                .Returns(Task.FromResult(0));

            //Act
            var result = await deviceService.Create(model);

            //Assert
            Assert.IsFalse(result is null);
        }
        [Test]
        public async Task UpdatePutTest()
        {
            //Arrange
            var deviceTypeId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var deviceid = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Superuser");
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            moqILoginService.Setup(p => p.GetCurrentCorporationId())
                .Returns(corporationId);
            repositoryMock.Setup(p => p.FindOrExceptionOneByConditionAsync<Device>(It.IsAny<Expression<Func<Device, bool>>>()))
                .Returns(Task.FromResult(new Device
                    {
                        DeviceId = deviceid,
                        Code = "AAAAAA",
                        CompanyId = companyId,
                        Company = new Company
                        {
                            CompanyId = companyId
                        },
                        StatusId = 3
                    }));
            requestFiltersMock.Setup(p => p.IsCompanyBelongToUser(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<String>()))
                .Returns(true);
            repositoryMock.Setup(p => p.GetAsQueryable<Device>())
                .Returns(new TestAsyncEnumerable<Device>(new List<Device>
                {
                    new Device
                    {
                        DeviceId = Guid.NewGuid(),
                        CompanyId = companyId,
                        StatusId = 3
                    }
                }.AsQueryable()));
            var model = new PutDevice
            {
                DeviceId = deviceid,
                Name = "TestDevice",
                Code = "AAAAAA",
                StatusId = 3,
                DeviceTypeId = deviceTypeId
            };
            repositoryMock.Setup(p => p.SaveAsync())
                .Returns(Task.FromResult(0));

            //Act
            var result = await deviceService.Update(model);

            //Assert
            Assert.IsFalse(result is null);
        }
        [Test]
        public async Task DeleteDeleteTest()
        {
            //Arrange
            var deviceTypeId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var deviceid = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Superuser");
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            moqILoginService.Setup(p => p.GetCurrentCorporationId())
                .Returns(corporationId);
            repositoryMock.Setup(p => p.FindOrExceptionOneByConditionAsync<Device>(It.IsAny<Expression<Func<Device, bool>>>()))
                .Returns(Task.FromResult(new Device
                    {
                        DeviceId = deviceid,
                        Code = "AAAAAA",
                        CompanyId = companyId,
                        Company = new Company
                        {
                            CompanyId = companyId
                        },
                        StatusId = 3
                    }));
            requestFiltersMock.Setup(p => p.IsCompanyBelongToUser(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<String>()))
                .Returns(true);
            repositoryMock.Setup(p => p.GetAsQueryable<Dialogue>())
                .Returns(new TestAsyncEnumerable<Dialogue>(new List<Dialogue>
                {
                    new Dialogue
                    {
                        DialogueId = Guid.NewGuid(),
                        DeviceId = Guid.NewGuid()
                    }
                }.AsQueryable()));
            repositoryMock.Setup(p => p.GetAsQueryable<Status>())
                .Returns(new TestAsyncEnumerable<Status>(new List<Status>
                {
                    new Status
                    {
                        StatusId = 4,
                        StatusName = "Disabled"
                    }
                }.AsQueryable()));
            repositoryMock.Setup(p => p.Delete(It.IsAny<Device>()));
            repositoryMock.Setup(p => p.SaveAsync())
                .Returns(Task.FromResult(0));

            //Act
            var result = await deviceService.Delete(deviceid);
            
            //Assert
            Assert.IsTrue(result == "Deleted");
        }
    }
}