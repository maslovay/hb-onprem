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

namespace BenchmarkScheduler.Tests
{
    [TestFixture]
    public class BenchmarkSchedulerTests : ServiceTest
    {
        private Process BenchmarkSchedulerProcess;
        private CompanyIndustry _industry;
        private RecordsContext _context;
        private Corporation corporation;
        private Company company;
        private Device device;
        private Dialogue dialogue;
        private DialogueVisual dialogueVisuals;
        private Client client;
        private Guid testFrameId;
        private List<WorkingTime> workingTimes;
        
        [SetUp]
        public async Task Setup()
        {
            await base.Setup(() =>
            {
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
                    BenchmarkSchedulerProcess = Process.Start(
                        "dotnet",
                        $"../../../../BenchmarkScheduler/bin/{config}/netcoreapp2.2/BenchmarkScheduler.dll --isCalledFromUnitTest true");
                }
                else
                {
                    BenchmarkSchedulerProcess = Process.Start(
                        "dotnet",
                        $"/app/HBOperations/BenchmarkScheduler/bin/{config}/netcoreapp2.2/BenchmarkScheduler.dll --isCalledFromUnitTest true");
                }
                Thread.Sleep(15000);
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
        }

        protected override async Task PrepareTestData()
        {
            try
            {
                var time = DateTime.Now.AddDays(-1);
                _industry = new CompanyIndustry()
                {
                    CompanyIndustryId = Guid.NewGuid(),
                    CompanyIndustryName = "testIndustryName",
                    SatisfactionIndex = 0,
                    LoadIndex = 0.5,
                    CrossSalesIndex = 0.04
                };
                corporation = new Corporation()
                {
                    Id = Guid.NewGuid(),
                    Name = "TestCorporation"
                };
                company = new Company()
                {
                    CompanyId = Guid.NewGuid(),
                    CorporationId = corporation.Id,
                    CompanyName = "testCompanyName",
                    CompanyIndustryId = _industry.CompanyIndustryId,
                    CreationDate = DateTime.Now,
                    IsExtended = false
                };
                System.Console.WriteLine($"company: {company.CompanyId}");
                device = new Device()
                {
                    DeviceId = Guid.NewGuid(),
                    Code = "TSTDEV",
                    Name = "TestDeviceForIntegrationTests",
                    CompanyId = company.CompanyId,
                    DeviceTypeId = Guid.Parse("b29a6c53-fbdf-4dba-930b-95a267e4e313"),
                    StatusId = 3
                };
                dialogue = new Dialogue()
                {
                    DialogueId = Guid.NewGuid(),
                    // ClientId = client.ClientId,
                    BegTime = time,
                    EndTime = time.AddMinutes(5),
                    CreationTime = time.AddMinutes(5),
                    DeviceId = device.DeviceId,
                    StatusId = 3,
                    Comment = "TestDialogue",
                    LanguageId = 2,
                    InStatistic = true
                };
                workingTimes = new List<WorkingTime>
                {
                    new WorkingTime
                    {
                        Day = 0,
                        CompanyId = company.CompanyId,
                        BegTime = new DateTime(01, 01, 01, 10, 00, 00),
                        EndTime = new DateTime(01, 01, 01, 19, 00, 00)
                    },
                    new WorkingTime
                    {
                        Day = 1,
                        CompanyId = company.CompanyId,
                        BegTime = new DateTime(01, 01, 01, 10, 00, 00),
                        EndTime = new DateTime(01, 01, 01, 19, 00, 00)
                    },
                    new WorkingTime
                    {
                        Day = 2,
                        CompanyId = company.CompanyId,
                        BegTime = new DateTime(01, 01, 01, 10, 00, 00),
                        EndTime = new DateTime(01, 01, 01, 19, 00, 00)
                    },
                    new WorkingTime
                    {
                        Day = 3,
                        CompanyId = company.CompanyId,
                        BegTime = new DateTime(01, 01, 01, 10, 00, 00),
                        EndTime = new DateTime(01, 01, 01, 19, 00, 00)
                    },
                    new WorkingTime
                    {
                        Day = 4,
                        CompanyId = company.CompanyId,
                        BegTime = new DateTime(01, 01, 01, 10, 00, 00),
                        EndTime = new DateTime(01, 01, 01, 19, 00, 00)
                    },
                    new WorkingTime
                    {
                        Day = 5,
                        CompanyId = company.CompanyId,
                        BegTime = new DateTime(01, 01, 01, 10, 00, 00),
                        EndTime = new DateTime(01, 01, 01, 19, 00, 00)
                    },
                    new WorkingTime
                    {
                        Day = 6,
                        CompanyId = company.CompanyId,
                        BegTime = new DateTime(01, 01, 01, 10, 00, 00),
                        EndTime = new DateTime(01, 01, 01, 10, 00, 00)
                    }
                };
                _repository.Create<CompanyIndustry>(_industry);
                _repository.Create<Corporation>(corporation);
                _repository.Create<Company>(company);
                _repository.Create<Dialogue>(dialogue);
                _repository.CreateRange<WorkingTime>(workingTimes);
                _repository.Create<Device>(device);
                _repository.Save();

            }
            catch(Exception e)
            {
                System.Console.WriteLine(e);
            }            
        }
        
        [Test]
        public async Task BenchmarkSchedulerTest()
        {
            //Arrange
             
            //Act
            var benchMarks = _repository.GetAsQueryable<Benchmark>().Where(p => p.IndustryId == _industry.CompanyIndustryId)
                .ToList();
            System.Console.WriteLine($"companyId: {company.CompanyId}");
            System.Console.WriteLine($"deviceId: {device.DeviceId}");
            System.Console.WriteLine($"industryId: {_industry.CompanyIndustryId}");
            System.Console.WriteLine($"benchMarks: {JsonConvert.SerializeObject(benchMarks)}");
            //Assert
            Assert.IsTrue(benchMarks.Any()); 
        }

        [TearDown]
        public async new Task TearDown()
        {
            await base.TearDown();    
            BenchmarkSchedulerProcess.Kill();        
        }
        protected override async Task CleanTestData()
        {   
            try
            {
                _repository.Delete<CompanyIndustry>(_industry);
                _repository.Delete<Corporation>(corporation);
                _repository.Delete<Company>(company);
                _repository.Delete<Device>(device);
                _repository.Delete<Dialogue>(dialogue);
                _repository.Delete<WorkingTime>(p => p.CompanyId == company.CompanyId);
                _repository.Delete<Benchmark>(p => p.IndustryId == _industry.CompanyIndustryId);
                _repository.Save();
            }
            catch(Exception e)
            {
                System.Console.WriteLine(e);
            }
        }
    }
}
