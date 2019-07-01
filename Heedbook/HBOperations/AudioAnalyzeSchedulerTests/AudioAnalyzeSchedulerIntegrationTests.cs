using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsrHttpClient;
using AudioAnalyzeScheduler;
using Common;
using HBData;
using HBData.Models;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NUnit.Framework;
using UnitTestExtensions;

namespace AudioAnalyseScheduler.Tests
{
    public class AudioAnalyzeSchedulerIntegrationTests : ServiceTest
    {
        private Dialogue _testDialog;
        private FileAudioDialogue _fileAudioDialogue;
        private Process _schedulerProcess;
        
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
            var config = "Release";

#if DEBUG
            config = "Debug";
#endif
            _schedulerProcess = Process.Start("dotnet",
                $"../../../../AudioAnalyzeScheduler/bin/{config}/netcoreapp2.2/AudioAnalyzeScheduler.dll --isCalledFromUnitTest true");
            Thread.Sleep(2000);
        }


        [TearDown]
        public async Task TearDown()
        {
            await base.TearDown();
        }
        
        protected override async Task PrepareTestData()
        {
            var currentDir = Environment.CurrentDirectory;

            _testDialog = CreateNewTestDialog();
            
            Console.WriteLine($"new test dialog id: {_testDialog.DialogueId}");
            
            _repository.AddOrUpdate(_testDialog);

            _fileAudioDialogue = new FileAudioDialogue()
            {
                FileAudioDialogueId = Guid.NewGuid(),
                DialogueId = _testDialog.DialogueId,
                STTResult = "[{\"Time\":6.72,\"Duration\":0.51,\"Word\":\"бессильно\"},{\"Time\":8.4,\"Duration\":0.58," +
                            "\"Word\":\"бежав\"},{\"Time\":9.75,\"Duration\":0.58,\"Word\":\"новоиспеченного\"}," +
                            "{\"Time\":10.54,\"Duration\":0.59,\"Word\":\"лейтенанта\"},{\"Time\":11.17,\"Duration\":0.45,\"Word\":\"гобсона\"}," +
                            "{\"Time\":11.65,\"Duration\":0.36,\"Word\":\"они\"},{\"Time\":12.89,\"Duration\":0.73,\"Word\":\"чувствительным\"}," +
                            "{\"Time\":17.29,\"Duration\":0.3,\"Word\":\"органом\"},{\"Time\":17.6,\"Duration\":0.45,\"Word\":\"ощупью\"}," +
                            "{\"Time\":18.06,\"Duration\":0.62,\"Word\":\"необычные\"},{\"Time\":18.76,\"Duration\":0.76,\"Word\":\"прибывали\"}," +
                            "{\"Time\":19.54,\"Duration\":0.32,\"Word\":\"ваши\"},{\"Time\":20.77,\"Duration\":0.56,\"Word\":\"одеяние\"}," +
                            "{\"Time\":21.85,\"Duration\":0.47,\"Word\":\"и\"},{\"Time\":22.35,\"Duration\":0.51,\"Word\":\"орёл\"}," +
                            "{\"Time\":23.23,\"Duration\":0.42,\"Word\":\"хотя\"},{\"Time\":24.58,\"Duration\":0.31,\"Word\":\"эти\"}," +
                            "{\"Time\":25.07,\"Duration\":0.35,\"Word\":\"для\"},{\"Time\":25.94,\"Duration\":0.27,\"Word\":\"её\"}," +
                            "{\"Time\":26.41,\"Duration\":0.2,\"Word\":\"ещё\"},{\"Time\":26.61,\"Duration\":0.44,\"Word\":\"ничего\"}," +
                            "{\"Time\":27.05,\"Duration\":0.36,\"Word\":\"но\"},{\"Time\":27.63,\"Duration\":0.7,\"Word\":\"девушка\"}," +
                            "{\"Time\":35.08,\"Duration\":0.58,\"Word\":\"человек\"},{\"Time\":37.23,\"Duration\":0.43,\"Word\":\"никак\"}," +
                            "{\"Time\":38.12,\"Duration\":0.19,\"Word\":\"не\"},{\"Time\":38.31,\"Duration\":0.57,\"Word\":\"пощадят\"}," +
                            "{\"Time\":40.14,\"Duration\":0.66,\"Word\":\"громоздкий\"},{\"Time\":40.8,\"Duration\":0.39,\"Word\":\"аппарат\"}," +
                            "{\"Time\":41.43,\"Duration\":0.4,\"Word\":\"давайте\"},{\"Time\":42.6,\"Duration\":0.63,\"Word\":\"разрушали\"}," +
                            "{\"Time\":43.23,\"Duration\":0.42,\"Word\":\"дома\"},{\"Time\":44.37,\"Duration\":0.35,\"Word\":\"завтра\"}," +
                            "{\"Time\":44.99,\"Duration\":0.46,\"Word\":\"пораньше\"},{\"Time\":46.38,\"Duration\":0.56,\"Word\":\"привозили\"}," +
                            "{\"Time\":47.36,\"Duration\":0.13,\"Word\":\"и\"},{\"Time\":48.0,\"Duration\":0.15,\"Word\":\"вы\"}," +
                            "{\"Time\":48.15,\"Duration\":0.15,\"Word\":\"мне\"},{\"Time\":48.3,\"Duration\":0.45,\"Word\":\"сказали\"},{\"Time\":49.26,\"Duration\":0.3,\"Word\":\"рук\"}," +
                            "{\"Time\":49.56,\"Duration\":0.48,\"Word\":\"лекарства\"},{\"Time\":54.57,\"Duration\":0.54,\"Word\":\"сегодня\"}," +
                            "{\"Time\":55.24,\"Duration\":0.35,\"Word\":\"завтра\"},{\"Time\":55.59,\"Duration\":0.24,\"Word\":\"утром\"}," +
                            "{\"Time\":55.83,\"Duration\":0.15,\"Word\":\"я\"},{\"Time\":55.98,\"Duration\":0.3,\"Word\":\"приду\"}," +
                            "{\"Time\":62.87,\"Duration\":0.09,\"Word\":\"к\"},{\"Time\":62.96,\"Duration\":0.33,\"Word\":\"нему\"}," +
                            "{\"Time\":63.69,\"Duration\":0.96,\"Word\":\"увеличить\"},{\"Time\":66.15,\"Duration\":0.62,\"Word\":\"исходит\"}," +
                            "{\"Time\":77.66,\"Duration\":0.24,\"Word\":\"из\"},{\"Time\":77.93,\"Duration\":0.31,\"Word\":\"вчера\"}," +
                            "{\"Time\":78.24,\"Duration\":0.51,\"Word\":\"ты\"},{\"Time\":79.1,\"Duration\":0.38,\"Word\":\"вчера\"}," +
                            "{\"Time\":82.01,\"Duration\":0.39,\"Word\":\"привезли\"},{\"Time\":83.2,\"Duration\":0.47,\"Word\":\"его\"}," +
                            "{\"Time\":84.48,\"Duration\":0.34,\"Word\":\"её\"},{\"Time\":89.67,\"Duration\":0.42,\"Word\":\"партнёров\"}," +
                            "{\"Time\":90.11,\"Duration\":0.41,\"Word\":\"подошла\"},{\"Time\":90.69,\"Duration\":0.48,\"Word\":\"утверждали\"}," +
                            "{\"Time\":91.26,\"Duration\":0.21,\"Word\":\"что\"},{\"Time\":91.54,\"Duration\":0.52,\"Word\":\"видели\"}," +
                            "{\"Time\":101.36,\"Duration\":0.49,\"Word\":\"радостно\"},{\"Time\":106.49,\"Duration\":0.43,\"Word\":\"должно\"}," +
                            "{\"Time\":107.27,\"Duration\":0.43,\"Word\":\"нежно\"},{\"Time\":107.79,\"Duration\":0.56,\"Word\":\"взрыва\"}," +
                            "{\"Time\":109.26,\"Duration\":0.25,\"Word\":\"едва\"},{\"Time\":110.33,\"Duration\":0.44,\"Word\":\"дарят\"}," +
                            "{\"Time\":111.18,\"Duration\":0.21,\"Word\":\"нам\"},{\"Time\":111.6,\"Duration\":0.32,\"Word\":\"пусть\"}," +
                            "{\"Time\":112.06,\"Duration\":0.3,\"Word\":\"должна\"},{\"Time\":112.47,\"Duration\":0.76,\"Word\":\"закрытую\"}," +
                            "{\"Time\":115.13,\"Duration\":0.38,\"Word\":\"банку\"}]",
                StatusId = 6,
                FileExist = false
            };
            
            _repository.AddOrUpdate(_fileAudioDialogue);
            _repository.Save();
        }

        protected override async Task CleanTestData()
        {
//            _repository.Delete(_testDialog);
//            _repository.Delete(_fileAudioDialogue);
//            _repository.Save();
        }

        protected override void InitServices()
        {
            _repository = ServiceProvider.GetService<IGenericRepository>();
        }
 
        [Test]
        public void EnsureCreatesDialogueSpeech()
        {
            Assert.IsTrue(WaitForSpeech());
        }

        [Test, Retry(3)]
        public void EnsureGetsPositiveShare()
        {
            GetPositiveShareInText();
        }

        private bool WaitForSpeech()
        {
            int deltaMs = 2000;
            int cntr = 0;
            
            while (cntr * deltaMs < 40000 || _repository.Get<DialogueSpeech>().All(ds => ds.DialogueId != _testDialog.DialogueId))
            {
                Thread.Sleep(deltaMs);
                ++cntr;
            }

            return _repository.Get<DialogueSpeech>().Any(ds => ds.DialogueId == _testDialog.DialogueId);
        }
        
//        [Test]
//        public void RecalcPositiveShare()
//        {
//            var result = 0.0;
//
//            var dialogs = _repository.GetWithInclude<Dialogue>(f => f.CreationTime >= DateTime.Now.AddDays(-7)
//                                                                    && f.DialogueSpeech.All(ds => ds.PositiveShare == 0.0),
//                f => f.DialogueSpeech, f => f.DialogueAudio).OrderByDescending(f => f.CreationTime).ToArray();
//
//            foreach (var ff in dialogs)
//            {
//                var fads = _repository.Get<FileAudioDialogue>().Where(x => x.DialogueId == ff.DialogueId && x.STTResult != null && x.STTResult.Length > 0);
//
//                foreach (var fad in fads)
//                {
//                    StringBuilder words = new StringBuilder();
//
//                    var asrResults = JsonConvert.DeserializeObject<List<AsrResult>>(fad.STTResult);
//                    if (asrResults.Any())
//                    {
//                        asrResults.ForEach(word =>
//                        {
//                            words.Append(" ");
//                            words.Append(word.Word);
//                        });
//                    }
//
//                    foreach (var speech in ff.DialogueSpeech)
//                    {
//                        if (speech == null)
//                            continue;
//
//                        var posShareStrg =
//                            RunPython.Run("GetPositiveShare.py", "./", "3", words.ToString());
//                        result = double.Parse(posShareStrg.Item1.Trim().Replace("\n", string.Empty));
//
//                        if (result > 0)
//                        {
//                            speech.PositiveShare = result;
//                            _repository.Update(speech);
//                        }
//                    }
//                }
//
//                _repository.Save();
//            }
//
//            Assert.Pass();
//        }
        
        private double GetPositiveShareInText()
        {
            var result = 0.0;

            var textsJson = GetTextResources("texts");

            foreach (var kvp in textsJson)
            {
                var posShareStrg = RunPython.Run("GetPositiveShare.py", "./", "3", kvp.Key);
		Console.WriteLine($"GetPosShare text: {posShareStrg.ToString()}");
		result = double.Parse(posShareStrg.Item1.Trim());
                if (kvp.Value == "positive")
                    Assert.GreaterOrEqual(result, 0.5);
                else
                    Assert.Less(result, 0.5);
            }

            return result;
        }
    }
}
