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
            var result = await provider.GetSlideShowsForOneDialogueAsync(TestData.GetDialoguesWithFrames().FirstOrDefault());

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(TestData.GetSlideShowSessions().Count(), result.Count());
        }
        [Test]
        public async Task GetSlideShowsForNullSlideShowSessionTest()
        {
            //arrange           
            repositoryMock.Setup(r => r.GetAsQueryable<SlideShowSession>()).Returns(TestData.GetEmptyList<SlideShowSession>());
            var provider = new AnalyticContentProvider(repositoryMock.Object);

            // Act
            var result = await provider.GetSlideShowsForOneDialogueAsync(TestData.GetDialoguesWithFrames().FirstOrDefault());

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [Test]
        public async Task GetSlideShowFilteredByPoolTest()
        {
            //arrange           
            repositoryMock.Setup(r => r.GetAsQueryable<SlideShowSession>()).Returns(TestData.GetSlideShowSessions());
            var provider = new AnalyticContentProvider(repositoryMock.Object);

            // Act
            var result = await provider.GetSlideShowFilteredByPoolAsync(
                        TestData.begDate, 
                        TestData.endDate, 
                        TestData.GetCompanyIds(), 
                        TestData.GetEmptyList<Guid>().ToList(),
                        TestData.GetEmptyList<Guid>().ToList(),
                        true);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
        }
        [Test]
        public async Task GetSlideShowFilteredIsNotPoolTest()
        {
            //arrange           
            repositoryMock.Setup(r => r.GetAsQueryable<SlideShowSession>()).Returns(TestData.GetSlideShowSessions());
            var provider = new AnalyticContentProvider(repositoryMock.Object);

            // Act
            var result = await provider.GetSlideShowFilteredByPoolAsync(
                      TestData.begDate,
                      TestData.endDate,
                      TestData.GetCompanyIds(),
                      TestData.GetEmptyList<Guid>().ToList(),
                      TestData.GetEmptyList<Guid>().ToList(),
                      false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
        }
        [Test]
        public async Task GetSlideShowFilteredByPoolIfNullTest()
        {
            //arrange           
            repositoryMock.Setup(r => r.GetAsQueryable<SlideShowSession>()).Returns(TestData.GetEmptyList<SlideShowSession>());
            var provider = new AnalyticContentProvider(repositoryMock.Object);

            // Act
            var result = await provider.GetSlideShowsForOneDialogueAsync(TestData.GetDialoguesWithFrames().FirstOrDefault());

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }
    }
}