using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBData.Models;
using HBData.Repository;
using HBLib;
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
        private readonly ElasticClientFactory _elasticClientFactory;
        private readonly GoogleConnector _googleConnector;
        public AudioAnalyze(
            IServiceScopeFactory factory,
            AsrHttpClient.AsrHttpClient asrHttpClient,
            ElasticClientFactory elasticClientFactory,
            GoogleConnector googleConnector
        )
        {
            // _repository = factory.CreateScope().ServiceProvider.GetService<IGenericRepository>();
            _context = factory.CreateScope().ServiceProvider.GetService<RecordsContext>();
            _asrHttpClient = asrHttpClient;
            _elasticClientFactory = elasticClientFactory;
            _googleConnector = googleConnector;
        }

        public async Task Run(String path)
        {
            var _log = _elasticClientFactory.GetElasticClient();
            _log.SetFormat("{Path}");
            _log.SetArgs(path);
            _log.Info("Function started");

            try
            {
                if (!String.IsNullOrWhiteSpace(path))
                {
                    var splitedString = path.Split('/');
                    var fileName = splitedString[1];
                    var dialogueId = Guid.Parse(Path.GetFileNameWithoutExtension(fileName));
                    var dialogue = _context.Dialogues
                        .FirstOrDefault(p => p.DialogueId == dialogueId);

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
                            Duration = dialogue.EndTime.Subtract(dialogue.BegTime).TotalSeconds
                        };

                        if (Environment.GetEnvironmentVariable("INFRASTRUCTURE") == "Cloud")
                        {
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
                                await _googleConnector.Recognize(blobGoogleDriveName, languageId, dialogueId.ToString(), true, true);
                            _log.Info("transaction id: " + transactionId);
                            if (transactionId == null || transactionId.Name <= 0)
                            {
                                throw new Exception("Transaction Id is null");
                            }
                            fileAudio.TransactionId = transactionId.Name.ToString();
                            fileAudio.StatusId = 6;
                        }
                        _context.FileAudioDialogues.Add(fileAudio);
                        _context.SaveChanges();
                        if (Environment.GetEnvironmentVariable("INFRASTRUCTURE") == "OnPrem")
                        {
                            await _asrHttpClient.StartAudioRecognize(dialogueId);
                        }
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