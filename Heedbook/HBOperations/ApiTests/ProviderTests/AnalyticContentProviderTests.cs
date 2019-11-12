using NUnit.Framework;
using System;
using System.Threading.Tasks;
using UserOperations.Providers;
using HBData.Models;
using System.Linq;

namespace ApiTests
{
    public class AnalyticContentProviderTests : ApiServiceTest
    {
        protected override void InitData()
        {
        }
        [SetUp]
        public new void Setup()
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
            Assert.AreEqual(3, result.Count());
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

        [Test]
        public async Task GetAnswersInOneDialogueTest()
        {
            //arrange           
            repositoryMock.Setup(r => r.GetAsQueryable<CampaignContentAnswer>()).Returns(TestData.GetCampaignContentsAnswers());
            var provider = new AnalyticContentProvider(repositoryMock.Object);

            // Act
            var result = await provider.GetAnswersInOneDialogueAsync(TestData.GetSlideShowInfos(), TestData.begDate, TestData.endDate, TestData.User1().Id);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
        }

        [Test]
        public async Task GetAnswersFullTest()
        {
            //arrange           
            repositoryMock.Setup(r => r.GetAsQueryable<CampaignContentAnswer>()).Returns(TestData.GetCampaignContentsAnswers());
            var provider = new AnalyticContentProvider(repositoryMock.Object);

            // Act
            var result = await provider.GetAnswersFullAsync(TestData.GetSlideShowInfos(),
                    TestData.begDate,
                    TestData.endDate,
                    TestData.GetCompanyIdsAll(),
                    TestData.GetEmptyList<Guid>().ToList(),
                    TestData.GetEmptyList<Guid>().ToList());

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count());
        }

        [Test]
        public void AddDialogueIdToShowTest()
        {
            //arrange           
            var provider = new AnalyticContentProvider(repositoryMock.Object);

            // Act
            var result = provider.AddDialogueIdToShow(TestData.GetSlideShowInfos(), TestData.GetDialogueInfoWithFrames().ToList());

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(TestData.GetSlideShowInfos().Count(), result.Count());
        }

        [Test]
        public void EmotionDuringAdvOneDialogueTest()
        {
            //arrange           
            var provider = new AnalyticContentProvider(repositoryMock.Object);

            // Act
            var result = provider.EmotionDuringAdvOneDialogue(
                TestData.GetSlideShowInfos(),
                TestData.GetDialogueInfoWithFrames().SelectMany(x => x.DialogueFrame).ToList());
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(60, result.Attention);
            Assert.That(Math.Abs((double)result.Negative - 0.3) < 0.01);
            Assert.That(Math.Abs((double)result.Positive - 0.4) < 0.01);
            Assert.That(Math.Abs((double)result.Neutral - 0.4) < 0.01);
        }
    }
}