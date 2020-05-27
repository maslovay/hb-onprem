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

namespace DetectFaceIdExtendedScheduler.Tests
{
    [TestFixture]
    public class DetectFaceIdExtendedSchedulerTests : ServiceTest
    {
        private Process _detectFaceIdExtendedSchedulerProcess;
        private Startup _startup;
        private CompanyIndustry _industry;
        private RecordsContext _context;
        private Company company;
        private Dialogue dialogue;
        private Client client;
        private Device _device;
        private DateTime _startFrameTime;
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
                    _detectFaceIdExtendedSchedulerProcess = Process.Start(
                        "dotnet",
                        $"../../../../DetectFaceIdExtendedScheduler/bin/{config}/netcoreapp2.2/DetectFaceIdExtendedScheduler.dll --isCalledFromUnitTest true");
                }
                else
                {
                    _detectFaceIdExtendedSchedulerProcess = Process.Start(
                    "dotnet",
                    $"/app/HBOperations/DetectFaceIdExtendedScheduler/bin/{config}/netcoreapp2.2/DetectFaceIdExtendedScheduler.dll --isCalledFromUnitTest true");
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
        }

        protected override async Task PrepareTestData()
        {
            try
            {                
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
                    IsExtended = true
                };
                _device = new Device()
                {
                    DeviceId = Guid.NewGuid(),
                    CompanyId = company.CompanyId,
                    Name = $"TestDevice"
                };
                client = new Client()
                {
                    ClientId = Guid.NewGuid(),
                    Name = "TestClient",
                    StatusId = 3,
                    CompanyId = company.CompanyId
                };
                _startFrameTime = DateTime.Now.AddDays(-1);
                var rnd = new Random();
                Guid? faceId = Guid.NewGuid();
                for(int i = 0; i < 50; i++)
                {
                    var Frame1 = new FileFrame()
                    {
                        FileFrameId = Guid.NewGuid(),
                        DeviceId = _device.DeviceId,
                        FileExist = true,
                        Time = _startFrameTime.AddSeconds(3 * i),
                        StatusNNId = 6,
                        FileName = $"{Guid.Empty}_{_device.DeviceId}_{_startFrameTime.AddMinutes(1).ToString("yyyyMMddHHmmss")}.jpg",
                        FileContainer = "frames",
                        IsFacePresent = true,
                        FaceLength = 1,
                        FaceId = rnd.Next(0,2) == 1 ? faceId : null
                    };
                    var frameAttribute1 = new FrameAttribute()
                    {
                        FrameAttributeId = Guid.NewGuid(),
                        FileFrameId = Frame1.FileFrameId,
                        Gender = "Male",
                        Age = 20,
                        Value = "{\"Top\":"+rnd.Next(300, 400)+",\"Width\":"+rnd.Next(120, 180)+",\"Height\":"+rnd.Next(120, 180)+",\"Left\":"+rnd.Next(150, 180)+"}",
                        Descriptor = $"[-0.44962623715400696,-0.61511456966400146,-0.14612722396850586,0.7983090877532959,1.6270284652709961,-0.31455427408218384,-0.82587963342666626,0.0015840530395507812,0.64044332504272461,-0.89610505104064941,-1.3372976779937744,0.81723946332931519,0.2418002188205719,-0.25880575180053711,0.741070032119751,-0.72313547134399414,0.50454646348953247,-0.037585347890853882,-0.39773634076118469,0.2915424108505249,0.5210498571395874,-0.75657522678375244,0.18684908747673035,-1.1302828788757324,-1.0956816673278809,0.68961769342422485,-0.086378574371337891,-0.070598721504211426,-0.34884560108184814,-0.090286523103713989,0.88897460699081421,0.4447893500328064,-0.88597822189331055,0.32945966720581055,0.016509056091308594,0.780862033367157,-0.66917681694030762,0.27675962448120117,1.4287997484207153,-0.52758115530014038,0.13456866145133972,-0.43690657615661621,-1.370867133140564,0.16370728611946106,-0.39696142077445984,0.65207850933074951,-0.56009817123413086,0.041393503546714783,-0.652790904045105,1.0415091514587402,0.933350145816803,-0.34185081720352173,-0.55455487966537476,0.6690526008605957,-0.18938949704170227,-0.082054644823074341,1.2438923120498657,-0.43882039189338684,-0.20405548810958862,-0.92328202724456787,0.30711287260055542,-0.36209535598754883,1.6927357912063599,0.63551735877990723,0.27123698592185974,0.2239094078540802,0.048714280128479004,0.59646749496459961,-1.1706740856170654,-0.21479709446430206,0.39797693490982056,-0.45408380031585693,-1.4848687648773193,0.061447456479072571,0.8486981987953186,-0.21212995052337646,-0.26407688856124878,0.16551004350185394,-1.0449566841125488,1.7355796098709106,-0.92636966705322266,-0.097459614276885986,-0.37012350559234619,0.64735370874404907,0.232021301984787,0.30317103862762451,0.13672752678394318,0.49866631627082825,-0.56965923309326172,0.42926427721977234,-0.96716147661209106,-0.39202034473419189,0.17719416320323944,0.93677908182144165,0.62649649381637573,0.59386640787124634,-0.93834608793258667,0.4807707667350769,-0.94846320152282715,0.42577067017555237,-0.64142793416976929,0.12930524349212646,0.66806352138519287,-0.40218645334243774,-0.76628577709198,-0.72495412826538086,0.66771978139877319,0.57908815145492554,-0.38447004556655884,0.28281921148300171,-0.89600569009780884,-1.3083962202072144,0.96925860643386841,-0.82208716869354248,-0.66177797317504883,-0.99651724100112915,-0.34671247005462646,1.0814461708068848,-0.36932450532913208,-1.0503139495849609,-0.47316426038742065,-0.98575276136398315,-0.90102720260620117,0.60182482004165649,1.006880521774292,0.91939443349838257,-0.37942749261856079,0.69284021854400635,-1.151688814163208,0.30068963766098022,-0.67988348007202148,-0.286417692899704,-0.84974825382232666,-1.9100472927093506,-0.54714876413345337,-0.081839576363563538,0.58376425504684448,1.5154246091842651,-0.99073445796966553,0.88446766138076782,0.75738281011581421,-0.965613842010498,-0.83895587921142578,-0.30662772059440613,0.31102654337882996,-0.84932267665863037,0.68352055549621582,-1.2435334920883179,-0.21397045254707336,0.860309898853302,-1.2222831249237061,0.33503207564353943,-0.33511483669281006,-0.38714200258255005,1.91568922996521,-1.0589002370834351,0.94949901103973389,1.3631411790847778,-0.67506200075149536,-0.29108786582946777,-0.30633068084716797,-0.44528022408485413,0.12287233024835587,-0.77709543704986572,-0.62592732906341553,-0.058701664209365845,1.0838463306427002,-0.55592858791351318,-1.985654354095459,-0.51176893711090088,-1.3928673267364502,1.1438907384872437,-0.1192222535610199,0.55354225635528564,-0.63224738836288452,1.4488880634307861,-0.36874693632125854,-0.8899080753326416,0.21035557985305786,-0.92706149816513062,-1.2088775634765625,-0.2275049090385437,-0.46789553761482239,0.74954438209533691,0.38798290491104126,1.0545274019241333,0.76152563095092773,-0.14842665195465088,-0.072959676384925842,-0.25909346342086792,0.79913210868835449,-0.056042157113552094,-0.59213221073150635,-0.58930683135986328,0.97320246696472168,-1.2590864896774292,0.94557631015777588,0.48506081104278564,-0.017931103706359863,0.7142866849899292,0.9517291784286499,0.29663717746734619,1.2166042327880859,0.019351720809936523,0.76695293188095093,1.3420464992523193,-0.37708526849746704,-0.10533140599727631,0.86221003532409668,-0.031086057424545288,-0.4096720814704895,-1.7132737636566162,0.72324877977371216,-0.4458356499671936,0.486851304769516,-0.48020941019058228,1.281645655632019,1.1885833740234375,-0.09583592414855957,0.98027586936950684,-1.6213494539260864,0.32341682910919189,-1.2628233432769775,0.30169683694839478,0.61793386936187744,-1.4848394393920898,0.626580536365509,1.1384243965148926,0.15022626519203186,-0.84981727600097656,1.174317479133606,0.30439466238021851,0.097476720809936523,-0.10917168855667114,0.83502054214477539,0.12136685848236084,-0.53970623016357422,0.10493394732475281,-0.64302605390548706,-0.45788693428039551,0.51773393154144287,-1.5552054643630981,-0.8707277774810791,0.70992201566696167,-1.4926223754882812,-0.70796060562133789,-0.5417940616607666,-0.25773090124130249,-0.014232560992240906,-0.20454928278923035,-1.0952463150024414,-1.2372488975524902,-0.3204687237739563,-0.92943143844604492,-0.54361975193023682,0.38981884717941284]"
                    };
                    _repository.Create<FileFrame>(Frame1);
                    _repository.Create<FrameAttribute>(frameAttribute1);
                }
                _repository.Create<CompanyIndustry>(_industry);
                _repository.Create<Client>(client);
                _repository.Create<Company>(company);
                _repository.Create<Device>(_device);
                _repository.Save();
            }
            catch(Exception e)
            {
                System.Console.WriteLine(e);
            }            
        }
        
        [Test]
        public async Task CheckDetectFaceIdExtendedScheduler()
        {
            //Arrange
            System.Console.WriteLine($"deviceId: {_device.DeviceId}");
            try
            {
                //Arrange
                Thread.Sleep(15000);
                //Act
                var fileFrames = _repository.GetAsQueryable<FileFrame>().Where(p => p.DeviceId == _device.DeviceId
                        && p.Time >= _startFrameTime
                        && p.FaceId == null)
                    .ToList();
                //Assert
                Assert.IsFalse(fileFrames.Any());
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
            _detectFaceIdExtendedSchedulerProcess.Kill();        
        }
        protected override async Task CleanTestData()
        {   
            try
            {
                _repository.Delete<CompanyIndustry>(_industry);
                _repository.Delete<Company>(company);
                _repository.Delete<Client>(client);
                _repository.Delete<Device>(_device);
                _repository.Delete<FrameAttribute>(p => p.FileFrame.DeviceId == _device.DeviceId);
                _repository.Delete<FileFrame>(p => p.DeviceId == _device.DeviceId);
                _repository.Save();
            }
            catch(Exception e)
            {
                System.Console.WriteLine(e);
            }
        }
    }
}