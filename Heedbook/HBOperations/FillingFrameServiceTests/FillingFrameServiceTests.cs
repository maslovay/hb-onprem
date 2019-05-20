using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;
using Common;
using FillingFrameService.Exceptions;
using HBData.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Resources;
using NUnit.Framework;
using RabbitMqEventBus.Events;
using Renci.SshNet.Messages;
using UnitTestExtensions;

namespace FillingFrameService.Tests
{
    [TestFixture]
    public class FillingFrameServiceTests : ServiceTest
    {
        private DialogueCreation _fillingFrameService;
        private Startup startup;
        private ResourceManager resourceManager;
        private DialogueCreationRun dialogCreationRun;
        private List<FileFrame> fileFrames = new List<FileFrame>(5);
        
        [SetUp]
        public async Task Setup()
        {
            await base.Setup(() =>
            {
                startup = new Startup(Config);
                startup.ConfigureServices(Services);
                StartupExtensions.MockRabbitPublisher(Services);
            }, true);
        }

        public async void TearDown()
        {
            await base.TearDown();
        }

        protected override async Task PrepareTestData()
        {
            fileFrames.Clear();
            
            var currentDir = Environment.CurrentDirectory;
            var testVideoFilepath = Directory
                .GetFiles(Path.Combine(currentDir, "Resources/Videos"), "testid*.mkv").FirstOrDefault();

            if (testVideoFilepath == null)
                throw new Exception("Can't get a test video for preparing a testset!");
            
            var testVideoFilename = Path.GetFileName(testVideoFilepath);
            
            var testVideoCorrectFileName = testVideoFilename?.Replace("testid", TestUserId.ToString());

            if (!(await _sftpClient.IsFileExistsAsync("videos/" + testVideoCorrectFileName)))
                await _sftpClient.UploadAsync(testVideoFilepath, "videos/", testVideoCorrectFileName);
            
            var testFramesFilepath = Directory
                .GetFiles(Path.Combine(currentDir, "Resources/Frames"), "testid*.jpg");
            
            if (testFramesFilepath.Length == 0)
                throw new Exception("Can't get test frames for preparing a testset!");

            
            var videoDateTime = GetDateTimeFromFileVideoName(testVideoCorrectFileName);
            
            // Create a dialog object
            dialogCreationRun = new DialogueCreationRun()
            {
                ApplicationUserId = TestUserId,
                DialogueId = Guid.NewGuid(),
                BeginTime = DateTime.MinValue,
                EndTime = videoDateTime.AddDays(10)
            };
            
            var newDialog = new Dialogue
            {
                DialogueId = dialogCreationRun.DialogueId,
                CreationTime = videoDateTime.AddDays(-10),
                BegTime = videoDateTime.AddDays(-10),
                EndTime = videoDateTime.AddDays(10),
                ApplicationUserId = TestUserId,
                LanguageId = null,
                StatusId = null,
                SysVersion = "",
                InStatistic = false,
                Comment = "test dialog!!!"
            };
            
            // filling frames 
            foreach (var testFramePath in testFramesFilepath )
            {
                var testFileFramePath = Path.GetFileName(testFramePath);
                var testFrameCorrectFileName = testFileFramePath
                    .Replace("testid", TestUserId.ToString());

                if (!(await _sftpClient.IsFileExistsAsync("frames/" + testFrameCorrectFileName)))
                    await _sftpClient.UploadAsync(testFramePath, "frames/", testFrameCorrectFileName);

                FileFrame testFileFrame;
                
                // if frame doesn't exist => let's create it!
                if (_repository.Get<FileFrame>().All(ff => ff.FileName != testFrameCorrectFileName))
                {
                    testFileFrame = new FileFrame
                    {
                        FileFrameId = Guid.NewGuid(),
                        ApplicationUserId = TestUserId,
                        FileExist = true,
                        FileName = testFrameCorrectFileName,
                        FileContainer = "frames",
                        StatusId = 5,
                        StatusNNId = null,
                        Time = GetDateTimeFromFileFrameName(testFrameCorrectFileName),
                        IsFacePresent = true,
                        FaceLength = null
                    };
                    await _repository.CreateAsync(testFileFrame);
                }
                else
                {
                    testFileFrame = _repository.Get<FileFrame>().First(ff => ff.FileName == testFrameCorrectFileName);
                    
                    // clean emotions and attributes in order to create new ones
                    _repository.Delete<FrameEmotion>( e => e.FileFrameId == testFileFrame.FileFrameId );
                    _repository.Delete<FrameAttribute>( a => a.FileFrameId == testFileFrame.FileFrameId );
                    await _repository.SaveAsync();
                }
                
                fileFrames.Add(testFileFrame);
                
                var newFrameEmotion = new FrameEmotion
                {
                    FrameEmotionId = Guid.NewGuid(),
                    FileFrameId = testFileFrame.FileFrameId,
                    AngerShare = 0.1,
                    ContemptShare = 0.04,
                    DisgustShare = 0.1,
                    HappinessShare = 0.6,
                    NeutralShare = 0.2,
                    SadnessShare = 0.1,
                    SurpriseShare = 0.8,
                    FearShare = 0.2,
                    YawShare = 0.3
                };

                var newFrameAttribute = new FrameAttribute
                {
                    FrameAttributeId = Guid.NewGuid(),
                    FileFrameId = testFileFrame.FileFrameId,
                    Gender = "Female",
                    Age = 25,
                    Value = "{\"Top\":328,\"Width\":285,\"Height\":413,\"Left\":430}",//resourceManager.GetString("TestAttributeValue"),
                    Descriptor = "[-0.39510998129844666,0.45138838887214661,-0.93791830539703369,0.502636194229126,-0.061470955610275269,-2.2355821132659912,0.75647497177124023," +
                                     "-1.310815691947937,0.79418540000915527,0.78868365287780762,-0.66683405637741089,-0.30441275238990784,0.074140980839729309,0.32231870293617249,-0.12536761164665222," +
                                     "0.36338400840759277,-0.35332748293876648,-0.731810450553894,0.67643547058105469,0.12396156787872314,-0.42743340134620667,0.7729794979095459,0.03435903787612915," +
                                     "-0.99971044063568115,-0.29785126447677612,0.92586982250213623,-0.34033998847007751,0.18002656102180481,1.8200640678405762,1.7962195873260498,0.03900514543056488," +
                                     "1.1390501260757446,-0.1087813526391983,0.55356943607330322,0.66431647539138794,-0.77211636304855347,0.66448891162872314,0.48593413829803467,-0.93749487400054932," +
                                     "0.84291356801986694,-0.55260151624679565,-0.32652947306632996,0.8213118314743042,-0.16672629117965698,0.27719569206237793,-0.49312996864318848,1.2503710985183716," +
                                     "0.24774102866649628,0.079429373145103455,-0.081706136465072632,0.84163820743560791,-1.7184140682220459,0.47058457136154175,-0.26676064729690552,-0.70080149173736572," +
                                     "-0.13795512914657593,-0.10474386066198349,0.36913317441940308,0.27458250522613525,-1.1078544855117798,-1.1436104774475098,0.16699677705764771,-2.9322559833526611," +
                                     "0.56968063116073608,-0.53763556480407715,0.23005671799182892,-1.6485106945037842,1.3451601266860962,0.75483572483062744,0.3275025486946106,-0.42906326055526733," +
                                     "-1.9974093437194824,0.30194738507270813,-0.52245396375656128,1.9646134376525879,-0.2281453013420105,0.24681791663169861,0.23235777020454407,0.61903882026672363," +
                                     "0.66429173946380615,0.07775609940290451,-0.098839014768600464,1.352681040763855,0.28308683633804321,0.48772534728050232,-0.024634379893541336,-0.95070517063140869," +
                                     "-0.29149481654167175,0.56766986846923828,-1.4489095211029053,1.0086709260940552,-0.05384141206741333,-1.1282298564910889,-0.58772087097167969,-0.15204767882823944," +
                                     "-0.17474347352981567,-0.59499084949493408,-0.664263129234314,-1.2561861276626587,0.18417841196060181,-0.61804431676864624,0.55099117755889893,-0.59885764122009277," +
                                     "-1.222140908241272,-2.1090669631958008,1.0950849056243896,-0.35024988651275635,0.28453490138053894,0.6970285177230835,-0.93674933910369873,-0.9092179536819458," +
                                     "-0.27327203750610352,-0.64797043800354,-0.013602472841739655,-0.65911144018173218,-0.57418090105056763,2.7127180099487305,-0.386361300945282,-1.3003029823303223," +
                                     "-0.58878242969512939,0.89787870645523071,1.3437532186508179,0.8377070426940918,0.37224367260932922,-1.9581402540206909,-0.56916463375091553,-0.54012519121170044," +
                                     "1.0942239761352539,0.82301247119903564,1.0742287635803223,1.8895776271820068,-0.63755714893341064,-0.44653370976448059,1.0413907766342163,0.78816068172454834," +
                                     "-0.43702617287635803,1.1565312147140503,-0.1180616095662117,0.31605690717697144,0.96229493618011475,0.17537054419517517,-0.87172919511795044,-0.21826986968517303," +
                                     "1.4195767641067505,-1.6134973764419556,-0.054766625165939331,0.047435954213142395,-1.104245662689209,0.10819640755653381,-0.078466594219207764,0.902820348739624," +
                                     "1.3889954090118408,-1.1588691473007202,-0.43578791618347168,-0.024932146072387695,0.13445404171943665,0.91743266582489014,-1.7263565063476562,0.22639544308185577," +
                                     "0.30604338645935059,-0.0655064731836319,-0.6587146520614624,-1.2089108228683472,-0.11414943635463715,0.17995423078536987,-1.0867414474487305,-0.67964857816696167," +
                                     "-0.55373549461364746,-1.3527023792266846,1.6210050582885742,0.15400309860706329,-0.41405123472213745,-0.49043542146682739,-0.083516888320446014,-1.049842357635498," +
                                     "1.0362175703048706,0.0162217915058136,-0.70101040601730347,-0.042348846793174744,0.80509853363037109,-0.30809086561203003,0.59989804029464722,0.17086076736450195," +
                                     "0.21134722232818604,-0.12951259315013885,-0.096953153610229492,-0.40701520442962646,-0.46897566318511963,-0.56224346160888672,0.73625439405441284,-0.4903697669506073," +
                                     "0.28605157136917114,0.1768372654914856,-0.99048161506652832,1.9571686983108521,-0.48811572790145874,0.284171998500824,2.0833094120025635,-2.2866075038909912,0.99815315008163452," +
                                     "1.4448859691619873,0.25737440586090088,0.063657820224761963,0.25600889325141907,-0.81576895713806152,0.19699141383171082,-1.1621417999267578,0.48206523060798645,0.62131857872009277," +
                                     "0.55725109577178955,-0.49302291870117188,-0.059052169322967529,-0.070593550801277161,0.0726487785577774,-0.136285662651062,-0.99394047260284424,-1.8364824056625366," +
                                     "0.94812756776809692,-0.33401522040367126,-0.36313295364379883,0.095659255981445312,0.6046563982963562,-1.8295621871948242,-0.72201204299926758,-1.5273559093475342," +
                                     "0.60042655467987061,0.061563253402709961,-0.46235924959182739,-0.21075139939785004,0.42317187786102295,-1.8115335702896118,-0.3715728223323822,-0.2776796817779541," +
                                     "0.31455147266387939,-0.91501307487487793,-0.73927962779998779,0.31331032514572144,-0.9611627459526062,-0.83587485551834106,-0.55539309978485107,0.39536175131797791," +
                                     "-1.5662624835968018,0.32425567507743835,0.52847295999526978,0.66103851795196533,0.10814327001571655,-0.81843221187591553,-0.26052659749984741,1.4084160327911377," +
                                     "-0.42523041367530823,-2.0672817230224609,-0.18054755032062531,0.21537443995475769,-0.2675987184047699,0.30104014277458191,-0.037798900157213211]"
                    //resourceManager.GetString("TestFaceDescriptor")
                };

                await _repository.CreateAsync(newFrameEmotion);
                await _repository.CreateAsync(newFrameAttribute);
            }

            await _repository.CreateAsync(newDialog);
            await _repository.SaveAsync();
        }

        protected override Task CleanTestData()
        {
            return null;
        }

        protected override void InitServices()
        {
            resourceManager = new ResourceManager("FillingFrameServiceTests.Resources.StringResources", 
                Assembly.GetExecutingAssembly());
            _fillingFrameService = ServiceProvider.GetService<DialogueCreation>();
        }

        [Test]
        public async Task EnsureCreatesDialogueFrameRecords()
        {
            await _fillingFrameService.Run(dialogCreationRun);
            
            Assert.IsTrue(_repository.Get<DialogueVisual>().Any(dv => dv.DialogueId == dialogCreationRun.DialogueId));
            Assert.IsTrue(_repository.Get<DialogueClientProfile>().Any(pr => pr.DialogueId == dialogCreationRun.DialogueId));

            var resultDialogFrames = _repository.Get<DialogueFrame>()
                .Where(df => df.DialogueId == dialogCreationRun.DialogueId);
            
            var resultEmotions = _repository.Get<FrameEmotion>()
                .Where(e => fileFrames.Any( ff => ff.FileFrameId == e.FileFrameId));

            Assert.AreEqual(resultDialogFrames.Count(), resultEmotions.Count());
        }
    }
}