using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using FillingFrameService.Exceptions;
using HBData.Models;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using RabbitMqEventBus.Events;

namespace FillingFrameService.Tests
{
    [TestFixture]
    public class FillingSatisfactionServiceTests : ServiceTest
    {
        private DialogueCreation _fillingFrameService;
        private Startup startup;
        
        [SetUp]
        public void Setup()
        {
            base.Setup(() =>
            {
                startup = new Startup(Config);
                startup.ConfigureServices(Services);
            });
        }

        protected override void InitServices()
        {
            _fillingFrameService = ServiceProvider.GetService<DialogueCreation>();
        }

        [Test]
        public async Task EnsureCreatesDialogueFrameRecord()
        {
            var frameAttributes = _repository.Get<FrameAttribute>().ToList(); // in order to make faster all this procedure
            var frameEmotions = _repository.Get<FrameEmotion>().ToList(); // in order to make faster all this procedure

            var processedFileFrame = _repository
                .Get<FileFrame>().FirstOrDefault(f => frameEmotions.Any(e => e.FileFrameId == f.FileFrameId) &&
                                                      frameAttributes.Any(a => a.FileFrameId == f.FileFrameId));

            Warn.If(() => processedFileFrame == null);

            if (processedFileFrame == null)
                return;
            
            var dialogCreationRun = new DialogueCreationRun()
            {
                ApplicationUserId = processedFileFrame.ApplicationUserId,
                DialogueId = Guid.NewGuid(),
                BeginTime = processedFileFrame.Time.AddMinutes(-5),
                EndTime = processedFileFrame.Time.AddMinutes(5)
            };

            foreach (var attribute in frameAttributes)
            {
                var fileExists = !await _sftpClient.IsFileExistsAsync("frames/" + attribute.FileFrame.FileName);
                Warn.If(!fileExists);
                if (!fileExists)
                    break;
            }

            await _fillingFrameService.Run(dialogCreationRun);
            
//            
//            
//            Assert.IsTrue(_repository.Get<DialogueClientSatisfaction>()
//                .Any(s => s.DialogueId == dialog.DialogueId));
        }

        [Test]
        public async Task CheckThrowsAnExceptionIfFileNotExists()
        {
            var frameAttributes = _repository.Get<FrameAttribute>().ToList(); // in order to make faster all this procedure
            var frameEmotions = _repository.Get<FrameEmotion>().ToList(); // in order to make faster all this procedure

            var processedFileFrame = _repository
                .Get<FileFrame>().FirstOrDefault(f => frameEmotions.Any(e => e.FileFrameId == f.FileFrameId) &&
                                                      frameAttributes.Any(a => a.FileFrameId == f.FileFrameId));

            Warn.If(() => processedFileFrame == null);

            if (processedFileFrame == null)
                return;
            
            var dialogCreationRun = new DialogueCreationRun()
            {
                ApplicationUserId = processedFileFrame.ApplicationUserId,
                DialogueId = Guid.NewGuid(),
                BeginTime = processedFileFrame.Time.AddMinutes(-5),
                EndTime = processedFileFrame.Time.AddMinutes(5)
            };

            foreach (var attribute in frameAttributes)
            {
                if (attribute.FileFrame == null) 
                    continue;
                
                var fileExists = !await _sftpClient.IsFileExistsAsync("frames/" + attribute.FileFrame.FileName);
                if (!fileExists)
                    Assert.Throws<DialogueCreationException>(async () =>
                        await _fillingFrameService.Run(dialogCreationRun));
            }
        }
    }
}