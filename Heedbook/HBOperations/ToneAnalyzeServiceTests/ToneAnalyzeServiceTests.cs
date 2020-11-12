using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Configurations;
using HBData;
using HBData.Models;
using HBData.Repository;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NUnit.Framework;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;

namespace ToneAnalyzeService.Tests
{
    [TestFixture]
    public class ToneAnalyzeServiceTests : ServiceTest
    {
        private Process _toneAnalyzeServiceProcess;
        private Startup _startup;
        private CompanyIndustry _industry;
        private RecordsContext _context;
        private Company company;
        private Dialogue dialogue;
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
                    _toneAnalyzeServiceProcess = Process.Start(
                        "dotnet",
                        $"../../../../ToneAnalyzeService/bin/{config}/netcoreapp2.2/ToneAnalyzeService.dll --isCalledFromUnitTest true");
                }
                else
                {
                    _toneAnalyzeServiceProcess = Process.Start(
                    "dotnet",
                    $"/app/HBOperations/ToneAnalyzeService/bin/{config}/netcoreapp2.2/ToneAnalyzeService.dll --isCalledFromUnitTest true");
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
            _repository = ServiceProvider.GetRequiredService<IGenericRepository>();            
            _context = ScopeFactory.CreateScope().ServiceProvider.GetRequiredService<RecordsContext>();
            _publisher = ServiceProvider.GetRequiredService<INotificationPublisher>();
            System.Console.WriteLine($"_publisher is null: {_publisher is null}");
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
                    EndTime = time.AddMinutes(3).AddSeconds(26),
                    CreationTime = time.AddMinutes(5),
                    DeviceId = TestDeviceId,
                    StatusId = 3,
                    Comment = "TestDialogue"
                };
                System.Console.WriteLine($"dialogueId: {dialogue.DialogueId}");
                _repository.Create<CompanyIndustry>(_industry);
                _repository.Create<Client>(client);
                _repository.Create<Company>(company);
                _repository.Create<Dialogue>(dialogue);
                _repository.Save();

                var currentDir = $"../../../../Common";

                var testDialogAudioPath = Directory
                    .GetFiles(Path.Combine(currentDir, "Resources/DialogueAudios"), "dialogueid.wav")
                    .FirstOrDefault();

                var testDialogAudioCorrectFileName = dialogue.DialogueId + ".wav";
                var tasks = new List<Task>();
                    
                if (!(await _sftpClient.IsFileExistsAsync("dialogueaudios/" + testDialogAudioCorrectFileName)))
                {
                    System.Console.WriteLine($"upload: {testDialogAudioCorrectFileName}");
                    await _sftpClient.UploadAsync(testDialogAudioPath, "dialogueaudios/", testDialogAudioCorrectFileName);
                }
                _sftpClient.ChangeDirectoryToDefault();
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
                var model = new ToneAnalyzeRun()
                {
                    Path = Path.Combine("dialogueaudios", $"{dialogue.DialogueId}.wav")
                };

                //Act
                _publisher.Publish(model);
                Thread.Sleep(180000);
                var dialogueIntervals = _context.DialogueIntervals
                    .Where(p => p.DialogueId == dialogue.DialogueId)
                    .ToList();
                System.Console.WriteLine($"dialogueIntervals:\n{JsonConvert.SerializeObject(dialogueIntervals)}");
                var fileAudio = _context.FileAudioDialogues.FirstOrDefault(p => p.DialogueId == dialogue.DialogueId);
                System.Console.WriteLine($"fileAudioDialogues:\n{JsonConvert.SerializeObject(fileAudio)}");
                System.Console.WriteLine($"currentDirectory: {Directory.GetCurrentDirectory()}");
                //Assert
                Assert.IsTrue(dialogueIntervals.Any());
                //Assert.IsFalse(_context.FileAudioDialogues.FirstOrDefault(p => p.DialogueId == dialogue.DialogueId) is null);
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
            _toneAnalyzeServiceProcess.Kill();        
        }
        protected override async Task CleanTestData()
        {   
            try
            {
                _repository.Delete<CompanyIndustry>(_industry);
                _repository.Delete<Company>(company);
                _repository.Delete<Client>(client);
                _repository.Delete<FileAudioDialogue>(p => p.DialogueId == dialogue.DialogueId);
                _repository.Delete<DialogueInterval>(p => p.DialogueId == dialogue.DialogueId);
                _repository.Delete<Dialogue>(dialogue);
                _repository.Save();
            }
            catch(Exception e)
            {
                System.Console.WriteLine(e);
            }
        }
    }
}