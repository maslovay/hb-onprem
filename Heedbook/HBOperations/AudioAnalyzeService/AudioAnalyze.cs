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
        private readonly GoogleConnector _googleConnector;
        private readonly ElasticClient _log;
        private readonly IGenericRepository _repository;

        public AudioAnalyze(GoogleConnector googleConnector,
            IServiceScopeFactory factory,
            ElasticClient log
        )
        {
            _googleConnector = googleConnector;
            _repository = factory.CreateScope().ServiceProvider.GetService<IGenericRepository>();
            _log = log;
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
                    var dialogueId = Path.GetFileNameWithoutExtension(fileName);
                    var languageId = Int32.Parse("2");

                    var currentPath = Directory.GetCurrentDirectory();
                    var token = await _googleConnector.GetAuthorizationToken(currentPath);

                    var blobGoogleDriveName =
                        dialogueId + "_client" + Path.GetExtension(fileName);
                    await _googleConnector.LoadFileToGoogleDrive(blobGoogleDriveName, path, token);
                    _log.Info("Load to disk");
                    await _googleConnector.MakeFilePublicGoogleCloud(blobGoogleDriveName, "./", token);
                    _log.Info("Make file public");
                    var transactionId =
                        await _googleConnector.Recognize(blobGoogleDriveName, languageId, dialogueId, true, true);
                    _log.Info("transaction id: " + transactionId);
                    var dialogue =
                        await _repository.FindOneByConditionAsync<Dialogue>(item =>
                            item.DialogueId == Guid.Parse(dialogueId));
                    var fileAudio = new FileAudioDialogue
                    {
                        DialogueId = Guid.Parse(dialogueId),
                        CreationTime = DateTime.UtcNow,
                        FileName = fileName,
                        StatusId = 6,
                        FileContainer = "dialogueaudios",
                        TransactionId = transactionId.Name.ToString(),
                        BegTime = dialogue.BegTime,
                        EndTime = dialogue.EndTime,
                        Duration = 15.0
                    };
                    await _repository.CreateAsync(fileAudio);
                    _repository.Save();
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