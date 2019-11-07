using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using UserOperations.Services;
using UserOperations.Controllers;
using UserOperations.AccountModels;
using HBData;
using System;
using System.Threading.Tasks;
using UserOperations.Providers;
using HBData.Models;
using HBData.Models.AccountViewModels;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Utils;
using UserOperations.Providers.Interfaces;
using UserOperations.Models.AnalyticModels;
using System.IO;
using HBData.Repository;
using System.Linq;

namespace ApiTests
{
    public class AnalyticContentProviderTests : ApiServiceTest
    {
        protected override void InitData()
        {
        }
        [SetUp]
        public void Setup()
        {
            base.Setup();
        }

        [Test]
        public async Task GetSlideShowsForOneDialogueTest()
        {
            //arrange           
            repositoryMock.Setup(r => r.GetAsQueryable<SlideShowSession>()).Returns(TestData.GetSlideShowSessions());

            var provider = new AnalyticContentProvider(repositoryMock.Object);

            // Act

            //var result = await provider.GetSlideShowsForOneDialogueAsync(TestData.GetDialoguesWithFrames().FirstOrDefault());

            // Assert
           // Assert.IsNotNull(result);
           // Assert.AreEqual(GetSlideShowInfos().Count(), result.Count());
        }
    }
}