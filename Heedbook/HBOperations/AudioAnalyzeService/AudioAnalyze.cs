using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBData.Models;
using HBData.Repository;
using HBLib.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AudioAnalyzeService
{
    public class AudioAnalyze
    {
        private readonly AsrHttpClient.AsrHttpClient _asrHttpClient;
        private readonly ElasticClient _log;
        private readonly RecordsContext _context;
        // private readonly IGenericRepository _repository;

        public AudioAnalyze(
            IServiceScopeFactory factory,
            ElasticClient log,
            AsrHttpClient.AsrHttpClient asrHttpClient
        )
        {
            // _repository = factory.CreateScope().ServiceProvider.GetService<IGenericRepository>();
            _context = factory.CreateScope().ServiceProvider.GetService<RecordsContext>();
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
                    var dialogue = _context.Dialogues
                        .Where(p => p.DialogueId == dialogueId)
                        .FirstOrDefault();

                        // await _repository.FindOneByConditionAsync<Dialogue>(item =>
                        //     item.DialogueId == dialogueId);
                    if (dialogue != null)
                    {
                        var fileAudios = _context.FileAudioDialogues.Where(p => p.DialogueId == dialogueId).ToList();
                        fileAudios.Where(p => p.StatusId != 6)
                            .ToList()
                            .ForEach(p => p.StatusId = 8);
                        
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
                        _context.FileAudioDialogues.Add(fileAudio);
                        _context.SaveChanges();
                        await _asrHttpClient.StartAudioRecognize(dialogueId);
                        _log.Info("Started recognize audio");
                        
                    }
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