using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBData.Models;
using HBData.Repository;
using HBLib;
using Notifications.Base;
using HBLib.Utils;
using Microsoft.EntityFrameworkCore;
using Notifications.Base;
using RabbitMqEventBus.Events;
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
        private readonly SftpClient _sftpclient;
        private readonly INotificationPublisher _publisher;
        private readonly INotificationHandler _handler;
        public AudioAnalyze(
            IServiceScopeFactory factory,
            INotificationPublisher publisher,
            AsrHttpClient.AsrHttpClient asrHttpClient,
            ElasticClientFactory elasticClientFactory,
            GoogleConnector googleConnector,
            SftpClient sftpclient,
            INotificationHandler handler
        )
        {
            try
            {
                // _repository = factory.CreateScope().ServiceProvider.GetService<IGenericRepository>();
                _context = factory.CreateScope().ServiceProvider.GetService<RecordsContext>();
                _asrHttpClient = asrHttpClient;
                _handler = handler;
                _elasticClientFactory = elasticClientFactory;
                _googleConnector = googleConnector;
                _sftpclient = sftpclient;
                _publisher = publisher;
            }
            catch
            {

            }
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
                    var containerName = splitedString[0];
                    var fileName = splitedString[1];
                    var dialogueId = Guid.Parse(Path.GetFileNameWithoutExtension(fileName));
                    var dialogue = _context.Dialogues
                        .FirstOrDefault(p => p.DialogueId == dialogueId);

                    var fileExist = await _sftpclient.IsFileExistsAsync(path);
                    if(!fileExist)
                    {
                        dialogue.Comment += " dialogue not have audio";
                        dialogue.StatusId = 8;
                        _context.SaveChanges();
                        _log.Info($"dialogue {dialogue.DialogueId} not have dialogueAudio");                        
                        return;
                    }
                    
                    if (dialogue != null)
                    {
                        var fileAudios = _context.FileAudioDialogues.Where(p => p.DialogueId == dialogueId
                                && p.FileContainer == containerName
                            ).ToList();
                        fileAudios.Where(p => p.StatusId != 6)
                            .ToList()
                            .ForEach(p => p.StatusId = 8);
                        
                        var fileAudio = new FileAudioDialogue
                        {
                            DialogueId = dialogueId,
                            CreationTime = DateTime.UtcNow,
                            FileName = fileName,
                            StatusId = 3,
                            FileContainer = containerName,
                            BegTime = dialogue.BegTime,
                            EndTime = dialogue.EndTime,
                            Duration = dialogue.EndTime.Subtract(dialogue.BegTime).TotalSeconds
                        };
                        await _googleConnector.CheckApiKey();
                        if (Environment.GetEnvironmentVariable("INFRASTRUCTURE") == "Cloud")
                        {
                            var languageId = Int32.Parse("2");

                            var currentPath = Directory.GetCurrentDirectory();
                            var token = await _googleConnector.GetAuthorizationToken(currentPath);
                            
                            var role = containerName == "dialogueaudios" ? "_client" : "_employee";
                            var blobGoogleDriveName =
                                dialogueId
                                + role
                                + Path.GetExtension(fileName);
                            await _googleConnector.LoadFileToGoogleDrive(blobGoogleDriveName, path, token);
                            _log.Info("Load to disk");
                            await _googleConnector.MakeFilePublicGoogleCloud(blobGoogleDriveName, "./", token);
                            _log.Info("Make file public");
                            var transactionId =
                                await _googleConnector.Recognize(blobGoogleDriveName, languageId, dialogueId.ToString(), true, true);
                            _log.Info("transaction id: " + transactionId);
                            if (transactionId == null || transactionId.Name <= 0)
                            {
                                _log.Info("transaction id is null. Possibly wrong api key");
                            }
                            else
                            {
                                fileAudio.TransactionId = transactionId.Name.ToString();
                                fileAudio.StatusId = 6;
                            }
                        }
                        _context.FileAudioDialogues.Add(fileAudio);
                        _context.SaveChanges();
                        if (Environment.GetEnvironmentVariable("INFRASTRUCTURE") == "OnPrem")
                        {
                            //await _asrHttpClient.StartAudioRecognize(dialogueId);
                            var message = new STTMessageRun{
                                Path = path
                            };
                            
                            _handler.EventRaised(message);
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