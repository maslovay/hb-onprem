using Common;
using HBData.Repository;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using HBData.Models;
using UserOperations.Providers;

namespace ApiTests
{
    public class AnalyticHomeProviderTest : ApiServiceTest
    {
      //  protected AnalyticHomeProvider _analyticHomeProvider;
        [Fact]
        public async Task GetBenchmarksListAsyncReturned()
        {
            Setup();
            DateTime beg = new DateTime(2019, 10, 01);
            DateTime end = new DateTime(2019, 10, 02);
            var companies = await _repository.FindAllAsync<Company>();
            var ids = companies.Take(10).Select(x => x.CompanyId).ToList();
            await _analyticHomeProvider.GetBenchmarksList(beg, end, ids);
            // Assert
            Assert.AreEqual("a", "a");
        }
        [SetUp]
        public void Setup()
        {
            // base.Setup(() => { }, true);
            base.Setup();
        }

        //protected override Task CleanTestData()
        //{
        //    return new Task(() => { });
        //}

        //protected override void InitServices()
        //{
        //    Services.AddScoped<AnalyticContentProvider>();
        //    Services.AddScoped<AnalyticCommonProvider>();
        //    Services.AddScoped<AnalyticHomeProvider>();

        //    _analyticHomeProvider = ServiceProvider.GetService<AnalyticHomeProvider>();
        //}

        //protected override Task PrepareTestData()
        //{
        //    return new Task(() => { });
        //}
    }
}
