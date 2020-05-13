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

namespace ApiTests
{
    public class SalesStageControllerTests : ApiServiceTest
    {
        private SalesStageService salesStageService;
        [SetUp]
        public new void Setup()
        {
            base.Setup();
            salesStageService = new SalesStageService(
                moqILoginService.Object,
                requestFiltersMock.Object,
                repositoryMock.Object);
        }
        [Test]
        public async Task GetAllGetTest()
        {
            //Arrange
            var deviceTypeId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var deviceid = Guid.NewGuid();
            var salesStageId = Guid.NewGuid();
            var phraseTypeId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Superuser");
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            moqILoginService.Setup(p => p.GetCurrentCorporationId())
                .Returns(corporationId);
            repositoryMock.Setup(p => p.FindOrExceptionOneByConditionAsync<Company>(It.IsAny<Expression<Func<Company, bool>>>()))
                .Returns(Task.FromResult(new Company
                    {
                        CompanyId = companyId,
                        CorporationId = corporationId
                    }));
            repositoryMock.Setup(p => p.GetAsQueryable<SalesStagePhrase>())
                .Returns(new TestAsyncEnumerable<SalesStagePhrase>(new List<SalesStagePhrase>
                {
                    new SalesStagePhrase
                    {
                        CompanyId = companyId,
                        CorporationId = corporationId,
                        Phrase = new Phrase
                        {
                            PhraseTypeId = phraseTypeId,
                            PhraseText = "TestPhraseText"
                        },
                        SalesStage = new SalesStage
                        {
                            SalesStageId = salesStageId,
                            Name = "TestSalesStage",
                            SequenceNumber = 3,
                        },

                    }
                }.AsQueryable()));
            repositoryMock.Setup(p => p.GetAsQueryable<PhraseType>())
                .Returns(new TestAsyncEnumerable<PhraseType>(new List<PhraseType>
                {
                    new PhraseType
                    {
                        PhraseTypeId = phraseTypeId,
                        Colour = "Red",
                        ColourSyn = "RedSync"
                    }
                }.AsQueryable()));

            //Act
            var result = await salesStageService.GetAll(companyId);

            //Assert
            Assert.IsFalse(result is null);
            Assert.IsTrue(result.Count > 0);
        }
    }
}