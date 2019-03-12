using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using HBLib.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace AudioAnalyzeService
{
    public class AudioAnalyze
    {
        private readonly GoogleConnector _googleConnector;
        private readonly IGenericRepository _repository;

        public AudioAnalyze(GoogleConnector googleConnector,
            IServiceScopeFactory scopeFactory
            )
        {
            _googleConnector = googleConnector;
            var scope = scopeFactory.CreateScope();
            _repository = scope.ServiceProvider.GetService<IGenericRepository>();
        }

        public async Task Run(String path)
        {
            Console.WriteLine("Function Audio STT started");
            /* */
            if (!String.IsNullOrWhiteSpace(path))
            {
                var splitedString = path.Split('/');
                var containerName = splitedString[0];
                var fileName = splitedString[1];
                var dialogueId = Path.GetFileNameWithoutExtension(fileName);
                var languageId = Int32.Parse("2");
                
                var currentPath = Directory.GetCurrentDirectory();
                var token = await _googleConnector.GetAuthorizationToken(currentPath);
                
                var blobGoogleDriveName =
                        dialogueId + "_client" + Path.GetExtension(fileName);
                await _googleConnector.LoadFileToGoogleDrive(blobGoogleDriveName, path, token);
                Console.WriteLine("Load to disk");
                await _googleConnector.MakeFilePublicGoogleCloud(blobGoogleDriveName, "./", token);
                Console.WriteLine("Make file public");
                var transactionId = await _googleConnector.Recognize(blobGoogleDriveName, languageId, dialogueId, true, true);
                Console.WriteLine("transaction id: " + transactionId);
                var dialogue = await _repository.FindOneByConditionAsync<Dialogue>(item => item.DialogueId == Guid.Parse(dialogueId));
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
                    Duration = 15.0,

                };
                await _repository.CreateAsync(fileAudio);
                _repository.Save();
            }
            Console.WriteLine("Function Audio STT finished");
        }
        
    }
}