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

namespace FillingHintService.Tests
{
    [TestFixture]
    public class FillingHintServiceTests : ServiceTest
    {
        private Process _fillingHintServiceTestsProcess;
        private Startup _startup;
        private CompanyIndustry _industry;
        private RecordsContext _context;
        private Company company;
        private Dialogue dialogue;
        private DialogueVisual dialogueVisuals;
        private Client client;
        private INotificationPublisher _publisher;
        
        [SetUp]
        public async Task Setup()
        {
            await base.Setup(() =>
            {
                Services.AddRabbitMqEventBus(Config);
                // _startup = new Startup(Config);
                // _startup.ConfigureServices(Services);    

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
                    _fillingHintServiceTestsProcess = Process.Start(
                        "dotnet",
                        $"../../../../FillingHintService/bin/{config}/netcoreapp2.2/FillingHintService.dll --isCalledFromUnitTest true");
                }
                else
                {
                    _fillingHintServiceTestsProcess = Process.Start(
                    "dotnet",
                    $"/app/HBOperations/FillingHintService/bin/{config}/netcoreapp2.2/FillingHintService.dll --isCalledFromUnitTest true");
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
                    Comment = "TestDialogue",
                    LanguageId = 2
                };
                dialogueVisuals = new DialogueVisual()
                {
                    DialogueVisualId = Guid.NewGuid(),
                    DialogueId = dialogue.DialogueId,
                    AttentionShare = 0.5
                };
                _repository.Create<CompanyIndustry>(_industry);
                _repository.Create<Client>(client);
                _repository.Create<Company>(company);
                _repository.Create<Dialogue>(dialogue);
                _repository.Create<DialogueVisual>(dialogueVisuals);
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
                var model = new FillingHintsRun()
                {
                    DialogueId = dialogue.DialogueId
                };
                System.Console.WriteLine($"dialogueId: {dialogue.DialogueId}");
                //Act
                _publisher.Publish(model);
                Thread.Sleep(30000);
                var _dialogueHints = _context.DialogueHints.Where(p => p.DialogueId == dialogue.DialogueId);

                //Assert
                Assert.IsTrue(_dialogueHints.Any());
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
            _fillingHintServiceTestsProcess.Kill();        
        }
        protected override async Task CleanTestData()
        {   
            try
            {
                _repository.Delete<CompanyIndustry>(_industry);
                _repository.Delete<Company>(company);
                _repository.Delete<Dialogue>(dialogue);
                _repository.Delete<DialogueVisual>(dialogueVisuals);
                _repository.Save();
            }
            catch(Exception e)
            {
                System.Console.WriteLine(e);
            }
        }
    }
}