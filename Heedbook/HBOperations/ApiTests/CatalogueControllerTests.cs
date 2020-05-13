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
    public class CatalogueControllerTests : ApiServiceTest
    {
        private CatalogueService catalogueService;
        [SetUp]
        public new void Setup()
        {
            base.Setup();
            catalogueService = new CatalogueService(
                repositoryMock.Object);
        }
        [Test]
        public async Task CountryGetTest()
        {
            //Arrange
            var countryId = Guid.NewGuid();
            repositoryMock.Setup(p => p.GetAsQueryable<Country>())
                .Returns(new TestAsyncEnumerable<Country>(new List<Country>
                {
                    new Country
                    {
                        CountryId = countryId,
                        CountryName = "TestCountry",
                        Company = new List<Company>
                        {
                            new Company
                            {
                                CompanyId = Guid.NewGuid(),
                                CompanyName = "TestCompany"
                            }
                        }
                    }
                }.AsQueryable()));

            //Act
            var countrys = catalogueService.CountrysGet();

            //Asser
            Assert.IsFalse(countrys.FirstOrDefault(p => p.CountryId == countryId) is null);
        }
        [Test]
        public async Task RolesGetTest()
        {
            //Arrange
            var roleId = Guid.NewGuid();
            
            repositoryMock.Setup(p => p.GetAsQueryable<ApplicationRole>())
                .Returns(new TestAsyncEnumerable<ApplicationRole>(new List<ApplicationRole>
                {
                    new ApplicationRole
                    {
                        UserRoles = new List<ApplicationUserRole>
                        {
                            new ApplicationUserRole
                            {
                                RoleId = roleId
                            }
                        }
                    }
                }));

            //Act
            var roles = catalogueService.RolesGet();

            //Asser
            Assert.IsFalse(roles.FirstOrDefault(p => p.UserRoles.FirstOrDefault().RoleId == roleId) is null);
        }
        [Test]
        public async Task DeviceTypeGetTest()
        {
            //Arrange
            var deviceTypeId = Guid.NewGuid();
            
            repositoryMock.Setup(p => p.GetAsQueryable<DeviceType>())
                .Returns(new TestAsyncEnumerable<DeviceType>(new List<DeviceType>
                {
                    new DeviceType
                    {
                        DeviceTypeId = deviceTypeId,
                        Name = "TestDeviceType"
                    }
                }));

            //Act
            var deviceTypes = (List<DeviceType>)catalogueService.DeviceTypeGet();

            //Asser
            Assert.IsFalse(deviceTypes is null);
        }
        [Test]
        public async Task IndustryGetTest()
        {
            //Arrange
            var industryId = Guid.NewGuid();
            
            repositoryMock.Setup(p => p.GetAsQueryable<CompanyIndustry>())
                .Returns(new TestAsyncEnumerable<CompanyIndustry>(new List<CompanyIndustry>
                {
                    new CompanyIndustry
                    {
                        CompanyIndustryId = industryId,
                        CompanyIndustryName = "testIndustry"
                    }
                }));

            //Act
            var industrys = catalogueService.IndustryGet();

            //Asser
            Assert.IsFalse(industrys.FirstOrDefault(p => p.CompanyIndustryId == industryId) is null);
        }
        [Test]
        public async Task LamguageGetTest()
        {
            //Arrange            
            repositoryMock.Setup(p => p.GetAsQueryable<Language>())
                .Returns(new TestAsyncEnumerable<Language>(new List<Language>
                {
                    new Language
                    {
                        LanguageId = 2,
                        LanguageName = "TestLanguage"
                    }
                }));

            //Act
            var languages = catalogueService.LanguageGet();

            //Asser
            Assert.IsFalse(languages.FirstOrDefault(p => p.LanguageId == 2) is null);
        }
        [Test]
        public async Task PhraseTypeTest()
        {
            //Arrange       
            var phraseTypeId = Guid.NewGuid();

            repositoryMock.Setup(p => p.GetAsQueryable<PhraseType>())
                .Returns(new TestAsyncEnumerable<PhraseType>(new List<PhraseType>
                {
                    new PhraseType
                    {
                        PhraseTypeId = phraseTypeId,
                        PhraseTypeText = "TestPhrase"
                    }
                }));

            //Act
            var phraseTypes = catalogueService.PhraseTypeGet();

            //Asser
            Assert.IsFalse(phraseTypes.FirstOrDefault(p => p.PhraseTypeId == phraseTypeId) is null);
        }
        [Test]
        public async Task AlertTypeTest()
        {
            //Arrange       
            var alertTypeId = Guid.NewGuid();

            repositoryMock.Setup(p => p.GetAsQueryable<AlertType>())
                .Returns(new TestAsyncEnumerable<AlertType>(new List<AlertType>
                {
                    new AlertType
                    {
                        AlertTypeId = alertTypeId,
                        Name = "TestAlertType"
                    }
                }));

            //Act
            var alertTypes = catalogueService.AlertTypeGet();

            //Asser
            Assert.IsFalse(alertTypes.FirstOrDefault(p => p.AlertTypeId == alertTypeId) is null);
        }
    }
}