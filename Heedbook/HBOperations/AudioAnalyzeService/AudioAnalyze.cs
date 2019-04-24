using System;
using System.IO;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using HBLib.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace AudioAnalyzeService
{
    public class AudioAnalyze
    {
        private readonly AsrHttpClient.AsrHttpClient _asrHttpClient;
        private readonly ElasticClient _log;
        private readonly IGenericRepository _repository;

        public AudioAnalyze(
            IServiceScopeFactory factory,
            ElasticClient log,
            AsrHttpClient.AsrHttpClient asrHttpClient
        )
        {
            _repository = factory.CreateScope().ServiceProvider.GetService<IGenericRepository>();
            _log = log;
            _asrHttpClient = asrHttpClient;
        }

        public async Task Run(String path)
        {
            try
            {
                _log.Info("Function Audio STT started");
                if (!String.IsNullOrWhiteSpace(path))
                {
                    var splitedString = path.Split('/');
                    var fileName = splitedString[1];
                    var dialogueId = Guid.Parse(Path.GetFileNameWithoutExtension(fileName));
                    var dialogue =
                        await _repository.FindOneByConditionAsync<Dialogue>(item =>
                            item.DialogueId == dialogueId);
                    var fileAudio = new FileAudioDialogue
                    {
                        DialogueId = dialogueId,
                        CreationTime = DateTime.UtcNow,
                        FileName = fileName,
                        StatusId = 3,
                        FileContainer = "dialogueaudios",
                        BegTime = dialogue.BegTime,
                        EndTime = dialogue.EndTime,
                        Duration = 15.0
                    };
                    await _repository.CreateAsync(fileAudio);
                    _repository.Save();
                    await _asrHttpClient.StartAudioRecognize(dialogueId);
                    _log.Info("Started recognize audio");
                }

                _log.Info("Function Audio STT finished");
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occured {e}");
                throw;
            }
        }
    }
}