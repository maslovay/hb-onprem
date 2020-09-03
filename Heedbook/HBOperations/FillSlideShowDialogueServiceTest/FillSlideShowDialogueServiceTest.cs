using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Configurations;
using HBData;
using HBData.Models;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NUnit.Framework;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;

namespace FillSlideShowDialogueService.Tests
{
    [TestFixture]
    public class FillSlideShowDialogueServiceTest : ServiceTest
    {
        private Process _fillSlideShowProcess;
        private Startup _startup;
        private CompanyIndustry _industry;
        private RecordsContext _context;
        private Company company;
        private Dialogue dialogue;
        private Content content;
        private Campaign campaign;
        private CampaignContent campaignContent;
        private SlideShowSession slideShowSession;
        private Client client;
        private INotificationPublisher _publisher;
        
        [SetUp]
        public async Task Setup()
        {
            await base.Setup(() =>
            {
                Services.AddRabbitMqEventBus(Config);
                _startup = new Startup(Config);
                _startup.ConfigureServices(Services);    

            }, true);
            RunServices();
        }
        private void RunServices()
        {
            try
            {
                var config = "Release";

#if DEBUG
                config = "Debug";
#endif
                var dockerEnvironment = Environment.GetEnvironmentVariable("DOCKER_INTEGRATION_TEST_ENVIRONMENT")=="TRUE" ? true : false;
                System.Console.WriteLine($"dockerEnvironment: {dockerEnvironment}");
                System.Console.WriteLine($"process folder: {config}");
                if(!dockerEnvironment)
                {
                    _fillSlideShowProcess = Process.Start(
                        "dotnet",
                        $"../../../../FillSlideShowDialogueService/bin/{config}/netcoreapp2.2/FillSlideShowDialogueService.dll --isCalledFromUnitTest true");
                }
                else
                {
                    _fillSlideShowProcess = Process.Start(
                    "dotnet",
                    $"/app/HBOperations/FillSlideShowDialogueService/bin/{config}/netcoreapp2.2/FillSlideShowDialogueService.dll --isCalledFromUnitTest true");
                }
                Thread.Sleep(20000);
            }
            catch(Exception e)
            {
                System.Console.WriteLine(e);
            }
        }
        protected override void InitServices()
        {
            System.Console.WriteLine($"InitServices");
            //_repository = ServiceProvider.GetRequiredService<IGenericRepository>();            
            _context = ScopeFactory.CreateScope().ServiceProvider.GetRequiredService<RecordsContext>();
            _publisher = ServiceProvider.GetRequiredService<INotificationPublisher>();
        }

        protected override async Task PrepareTestData()
        {
            try
            {
                var time = DateTime.Now;
                _industry = new CompanyIndustry()
                {
                    CompanyIndustryId = Guid.NewGuid(),
                    CompanyIndustryName = "testIndustryName",
                    SatisfactionIndex = 0,
                    LoadIndex = 0.5,
                    CrossSalesIndex = 0.04
                };
                company = new Company()
                {
                    CompanyId = Guid.NewGuid(),
                    CompanyName = "testCompanyName",
                    CompanyIndustryId = _industry.CompanyIndustryId,
                    CreationDate = DateTime.Now,
                    IsExtended = false
                };
                client = new Client()
                {
                    ClientId = Guid.NewGuid(),
                    Name = "TestClient",
                    StatusId = 3,
                    CompanyId = company.CompanyId
                };
                dialogue = new Dialogue()
                {
                    DialogueId = Guid.NewGuid(),
                    ClientId = client.ClientId,
                    BegTime = time,
                    EndTime = time.AddMinutes(5),
                    CreationTime = time.AddMinutes(5),
                    DeviceId = TestDeviceId,
                    StatusId = 3,
                    Comment = "TestDialogue"
                };
                content = new Content()
                {
                    ContentId = Guid.NewGuid(),
                    Name = "Тестовый контент",
                    Duration = 30,
                    CompanyId = company.CompanyId,
                    CreationDate = time.AddDays(-1),
                    StatusId = 3,
                    RawHTML = "<div></div>"
                };
                campaign = new Campaign()
                {
                    CampaignId = Guid.NewGuid(),
                    Name = $"Тестовая кампания",
                    BegDate = time.AddDays(-1),
                    EndDate = time.AddDays(1),
                    CreationDate = time.AddDays(-2),
                    CompanyId = company.CompanyId,
                    StatusId = 3,
                };
                campaignContent = new CampaignContent()
                {
                    CampaignContentId = Guid.NewGuid(),
                    ContentId = content.ContentId,
                    CampaignId = campaign.CampaignId,
                    StatusId = 3
                };
                slideShowSession = new SlideShowSession()
                {
                    SlideShowSessionId = Guid.NewGuid(),
                    BegTime = time.AddSeconds(10),
                    EndTime = time.AddSeconds(40),
                    CampaignContentId = campaignContent.CampaignContentId,
                    DeviceId = TestDeviceId
                };
                System.Console.WriteLine($"slideShowSession in Test:\n{JsonConvert.SerializeObject(slideShowSession)}");
                _repository.Create<CompanyIndustry>(_industry);
                _repository.Create<Client>(client);
                _repository.Create<Company>(company);
                _repository.Create<Dialogue>(dialogue);
                _repository.Create<Content>(content);
                _repository.Create<Campaign>(campaign);
                _repository.Create<CampaignContent>(campaignContent);
                _repository.Create<SlideShowSession>(slideShowSession);
                _repository.Save();
            }
            catch(Exception e)
            {
                System.Console.WriteLine(e);
            }            
        }
        
        [Test]
        public async Task CheckSoundFilePresents()
        {
            //Arrange
                 
            try
            {
                var model = new FillSlideShowDialogueRun()
                {
                    DialogueId = dialogue.DialogueId
                };

                //Act
                _publisher.Publish(model);
                Thread.Sleep(30000);
                var _slideShowSession = _context.SlideShowSessions
                    .FirstOrDefault(p => p.SlideShowSessionId == slideShowSession.SlideShowSessionId);
                System.Console.WriteLine($"_slideShowSession is null: {_slideShowSession is null}");
                System.Console.WriteLine($"_slideShowSession Check: {JsonConvert.SerializeObject(_slideShowSession)}");
                //Assert
                Assert.IsFalse(_slideShowSession.DialogueId is null);
            } 
            catch(Exception e)
            {
                System.Console.WriteLine(e);
            }
        }
        [TearDown]
        public async new Task TearDown()
        {
            await base.TearDown();    
            _fillSlideShowProcess.Kill();        
        }
        protected override async Task CleanTestData()
        {   
            try
            {
                _repository.Delete<CompanyIndustry>(_industry);
                _repository.Delete<Company>(company);
                _repository.Delete<Dialogue>(dialogue);
                _repository.Delete<Content>(content);
                _repository.Delete<Campaign>(campaign);
                _repository.Delete<CampaignContent>(campaignContent);
                _repository.Delete<SlideShowSession>(slideShowSession);
                _repository.Save();
            }
            catch(Exception e)
            {
                System.Console.WriteLine(e);
            }
        }
    }
}