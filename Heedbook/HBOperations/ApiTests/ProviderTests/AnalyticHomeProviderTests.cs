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
using HBData.Models;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using System.Threading;
using Microsoft.EntityFrameworkCore.Query.Internal;
using UserOperations.Models.AnalyticModels;
using UserOperations.Models.Get;

namespace ApiTests
{
    public class AnalyticHomeProviderTests : ApiServiceTest
    {
        [SetUp]
        public new void Setup()
        {
            base.Setup();
        }
        
        [Test]
        public async Task GetBenchmarksListTest()
        {
            ////Arrange
            //var begTime = new DateTime(2019, 11, 10, 12, 00, 00);
            //var endTime = new DateTime(2019, 11, 14, 12, 00, 00);
            //var companyIds = new List<Guid>
            //{
            //    new Guid("55b74216-7871-4f5b-b21f-9bcf5177a121"),
            //    new Guid("55b74216-7871-4f5b-b21f-9bcf5177a122"),
            //    new Guid("55b74216-7871-4f5b-b21f-9bcf5177a123")
            //};
            //var benchmarks = TestData.GetBenchmarks();
            //var benchmarkNames = TestData.GetBenchmarkName();
            //var companys = new List<Company>
            //    {
            //        new Company{CompanyIndustryId = new Guid("15b74216-7871-4f5b-b21f-9bcf5177a12b")},
            //        new Company{CompanyIndustryId = new Guid("15b74216-7871-4f5b-b21f-9bcf5177a12b")}
            //    }.AsEnumerable();
            //repositoryMock.Setup(p => p.Get<Benchmark>()).Returns(benchmarks);
            //repositoryMock.Setup(p => p.Get<BenchmarkName>()).Returns(benchmarkNames);
            //repositoryMock.Setup(p => p.FindByConditionAsync<Company>(It.IsAny<Expression<Func<Company,bool>>>()))
            //    .Returns(Task.FromResult<IEnumerable<Company>>(companys));
            //var analyticHomeProvider = new AnalyticHomeProvider(repositoryMock.Object);

            ////Act
            //var res = await analyticHomeProvider.GetBenchmarksList(begTime, endTime, companyIds);
            //var result = res.ToList();

            ////Assert
            //Assert.AreEqual(result.Count, 3);
        }
        [Test]
        public void GetBenchmarkIndustryAvgTest()
        {
            //Arrange
            //var benchmarkName = "SatisfactionIndexIndustryAvg";
            //var benchmarksList = new List<BenchmarkModel>()
            //{
            //    new BenchmarkModel{Name = benchmarkName, Value = 80},
            //    new BenchmarkModel{Name = benchmarkName, Value = 70},
            //    new BenchmarkModel{Name = benchmarkName, Value = 60},
            //};
            

            ////Act
            //var analyticHomeProvider = new AnalyticHomeProvider(repositoryMock.Object);
            //var result = analyticHomeProvider.GetBenchmarkIndustryAvg(benchmarksList, benchmarkName);

            ////Assert
            //Assert.AreEqual(result, 70);
        }
        [Test]
        public void GetBenchmarkIndustryMaxTest()
        {
            //Arrange
            //var benchmarkName = "SatisfactionIndexIndustryAvg";
            //var benchmarksList = TestData.GetBenchmarkModels(benchmarkName);            

            ////Act
            //var analyticHomeProvider = new AnalyticHomeProvider(repositoryMock.Object);
            //var result = analyticHomeProvider.GetBenchmarkIndustryMax(benchmarksList, benchmarkName);

            ////Assert
            //Assert.AreEqual(result, 80);
        }
        [Test]
        public async Task GetIndustryIdsAsyncTest()
        {
            ////Arrange
            //var companyIds = new List<Guid>
            //{
            //    new Guid("55b74216-7871-4f5b-b21f-9bcf5177a121"),
            //    new Guid("55b74216-7871-4f5b-b21f-9bcf5177a122"),
            //    new Guid("55b74216-7871-4f5b-b21f-9bcf5177a123")
            //};
            //var companys = new List<Company>
            //{
            //    new Company
            //    {
            //        CompanyId = new Guid("55b74216-7871-4f5b-b21f-9bcf5177a121"),
            //        CompanyIndustryId = new Guid("15b74216-7871-4f5b-b21f-9bcf5177a12b")
            //    },
            //    new Company
            //    {
            //        CompanyId = new Guid("55b74216-7871-4f5b-b21f-9bcf5177a122"),
            //        CompanyIndustryId = new Guid("25b74216-7871-4f5b-b21f-9bcf5177a12b")
            //    }
            //}.AsEnumerable();
            //repositoryMock.Setup(p => p.FindByConditionAsync<Company>(It.IsAny<Expression<Func<Company, bool>>>()))
            //    .Returns(Task.FromResult<IEnumerable<Company>>(companys));

            ////Act
            //var analyticHomeProvider = new AnalyticHomeProvider(repositoryMock.Object);
            //var res = await analyticHomeProvider.GetIndustryIdsAsync(companyIds);
            //var result = res.ToList();

            ////Assert
            //Assert.AreEqual(result.Count, 2);
        }
    }
}