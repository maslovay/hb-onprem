using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Utils;
using Configurations;
using DialogueStatusCheckerScheduler.Tests.Handlers;
using HBData.Models;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;

namespace DialogueStatusCheckerScheduler.Tests
{
    public class DialogueStatusCheckerSchedulerIntegrationTests : ServiceTest
    {
        private Process _schedulerProcess;
        private StubService _stubService;
        private Dialogue _testDialog;
        private readonly FileAudioDialogue _fileAudioDialogue;
        private INotificationPublisher _publisher;
        
        [SetUp]
        public async Task Setup()
        {
            await base.Setup(() =>
            {
                Services.AddRabbitMqEventBus(Config);
                Services.AddScoped<INotificationPublisher, NotificationPublisher>();
                Services.AddSingleton<StubService>();
            }, true);

            _publisher = ServiceProvider.GetService<INotificationPublisher>();
            _stubService = ServiceProvider.GetService<StubService>();
            _publisher.Subscribe<FillingSatisfactionRun, FillingSatisfactionRunHandler>();
            RunServices();
        }
        
        private void RunServices()
        {
            var config = "Release";

#if DEBUG
            config = "Debug";
#endif
            _schedulerProcess = Process.Start("dotnet",
                $"../../../../DialogueStatusCheckerScheduler/bin/{config}/netcoreapp2.2/DialogueStatusCheckerScheduler.dll --isCalledFromUnitTest true");
        }

        [TearDown]
        public async new Task TearDown()
        {
            await base.TearDown();
            StopServices();
        }

        private void StopServices()
        {
            try
            {
                _schedulerProcess.Kill();
            }
            catch (Exception ex)
            {
                
            }
        }

        protected override async Task PrepareTestData()
        {
           var currentDir = Environment.CurrentDirectory;

            _testDialog = CreateNewTestDialog( -2 );

            var dialogueFrame =  ModelsFactory.Generate<DialogueFrame>(df => df.DialogueId = _testDialog.DialogueId);
            dialogueFrame.DialogueFrameId = Guid.NewGuid();
            var dialogueAudio = ModelsFactory.Generate<DialogueAudio>(da => da.DialogueId = _testDialog.DialogueId);
            dialogueAudio.DialogueAudioId = Guid.NewGuid();
            var dialogueInterval =  ModelsFactory.Generate<DialogueInterval>(di => di.DialogueId = _testDialog.DialogueId);
            dialogueInterval.DialogueIntervalId = Guid.NewGuid();
            var dialogueVisual = ModelsFactory.Generate<DialogueVisual>(dv => dv.DialogueId = _testDialog.DialogueId);
            dialogueVisual.DialogueVisualId = Guid.NewGuid();
            var dialogueClientProfile = ModelsFactory.Generate<DialogueClientProfile>(dcp => dcp.DialogueId = _testDialog.DialogueId);
            dialogueClientProfile.DialogueClientProfileId = Guid.NewGuid();

            _repository.AddOrUpdate(_testDialog);
            _repository.AddOrUpdate(dialogueFrame);
            _repository.AddOrUpdate(dialogueAudio);
            _repository.AddOrUpdate(dialogueInterval);
            _repository.AddOrUpdate(dialogueVisual);
            _repository.AddOrUpdate(dialogueClientProfile);
            
            _repository.Save();
            
            Console.WriteLine($"new test dialog id: {_testDialog.DialogueId}");

            _repository.Save();
        }

        protected override async Task CleanTestData()
        {
        }

        protected override void InitServices()
        {
        }
        
        [Test, Retry(3)]
        public void EnsureCallsFillingSatisfaction()
        {
            Assert.IsTrue(WaitForAFlag());
            
            StopServices();
        }

        private bool WaitForAFlag()
        {
            int deltaMs = 2000;
            int cntr = 0;
            
            while (cntr * deltaMs < 40000 || !_stubService.Flag)
            {
                Thread.Sleep(deltaMs);
                ++cntr;
            }

            return _stubService.Flag;
        }
    }
}