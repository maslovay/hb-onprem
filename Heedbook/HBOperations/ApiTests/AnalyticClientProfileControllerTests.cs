using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using UserOperations.Controllers;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Services;
using HBData.Models;

namespace ApiTests
{
    public class AnalyticClientProfileServiceTests : ApiServiceTest
    {   
        private AnalyticClientProfileService _analyticClientProfileService;
        [SetUp]
        public new void Setup()
        {
            base.Setup();
            _analyticClientProfileService = new AnalyticClientProfileService(
                moqILoginService.Object,
                requestFiltersMock.Object,
                repositoryMock.Object                
            );
        }      
        
        [Test]
        public async Task EfficiencyDashboard()
        {
            //Arrange
            var dialogues = TestData.GetDialoguesSimple();
            var companyId = Guid.NewGuid();
            var list = new List<Guid>(){companyId};
            
            moqILoginService.Setup(p => p.GetCurrentCompanyId()).Returns(companyId);
            requestFiltersMock.Setup(p => p.CheckRolesAndChangeCompaniesInFilter(ref list, It.IsAny<List<Guid>>(), It.IsAny<string>(), It.IsAny<Guid>()));
            // filterMock.Setup(p => p.CheckRolesAndChangeCompaniesInFilter(ref It.Ref<List<Guid>>.IsAny, It.IsAny<List<Guid>>(), It.IsAny<string>(), It.IsAny<Guid>()))
            //     .Callback(new CheckRolesAndChangeCompaniesInFilter((ref List<Guid> companyIds, List<Guid> corporationIds, string ApplicationRole, Guid token) => {companyIds = list;}));
            var dateTimeNow = DateTime.Now.Date;
            requestFiltersMock.Setup(p => p.GetBegDate(It.IsAny<string>()))
               .Returns(dateTimeNow.AddDays(-5));
            requestFiltersMock.Setup(p => p.GetEndDate(It.IsAny<string>()))
               .Returns(dateTimeNow);
            var applicaionUserId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            repositoryMock.Setup(p => p.GetAsQueryable<Dialogue>())
               .Returns(new TestAsyncEnumerable<Dialogue>(
                    new List<Dialogue>()
                    {
                        new Dialogue()
                        {
                            DialogueId = Guid.NewGuid(),
                            ClientId = Guid.NewGuid(),
                            CreationTime = DateTime.Now,
                            BegTime = DateTime.Now.AddMinutes(-5),
                            EndTime = DateTime.Now,
                            DeviceId = deviceId,
                            Device = new Device{
                                DeviceId = deviceId,
                                CompanyId = companyId
                            },
                            StatusId = 3,
                            InStatistic = true,
                            ApplicationUserId = applicaionUserId,
                            ApplicationUser = new ApplicationUser()
                            {
                                Id = applicaionUserId
                            },
                            DialogueClientProfile = new List<DialogueClientProfile>
                            {
                                new DialogueClientProfile
                                {
                                    Age = 20, 
                                    Gender = "male"
                                }
                            }
                        }
                    }));

            //Act
            var result = await _analyticClientProfileService.EfficiencyDashboard(
                DateTime.Now.Date.AddDays(-5).ToString("yyyyMMdd"),
                DateTime.Now.ToString("yyyyMMdd"),
                new List<Guid?>{applicaionUserId}, 
                new List<Guid>{companyId}, 
                TestData.GetGuids(),
                new List<Guid>{deviceId}
            );
            var dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(result);

            //Assert
            Assert.IsTrue(dictionary.ContainsKey("allClients"));
            Assert.IsTrue(dictionary.ContainsKey("uniquePerYearClients"));
            Assert.IsTrue(dictionary.ContainsKey("genderAge"));
        }
    }    
}     