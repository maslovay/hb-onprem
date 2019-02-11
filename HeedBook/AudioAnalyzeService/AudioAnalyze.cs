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

        private readonly IConfiguration _configuration;

        private readonly IGenericRepository _repository;
        
        public AudioAnalyze(GoogleConnector googleConnector,
            IServiceScopeFactory scopeFactory)
        {
            _googleConnector = googleConnector;
            var scope = scopeFactory.CreateScope();
            _repository = scope.ServiceProvider.GetRequiredService<IGenericRepository>();
        }

        public async Task Run(String path)
        {
            if (!String.IsNullOrWhiteSpace(path))
            {
                var splitedString = path.Split('/');
                var containerName = splitedString[0];
                var fileName = splitedString[1];
                var dialogueId = Path.GetFileNameWithoutExtension(fileName);
                var languageId = Int32.Parse("2");
                
                var token = await _googleConnector.GetAuthorizationToken("./");
                
                var blobGoogleDriveName =
                        dialogueId + "_client" + Path.GetExtension(fileName);
                await _googleConnector.LoadFileToGoogleDrive(blobGoogleDriveName, path, token);
                await _googleConnector.MakeFilePublicGoogleCloud(blobGoogleDriveName, "./", token);
                var result = await _googleConnector.Recognize(blobGoogleDriveName, languageId, true,
                    true);
                var deserializedResult = JsonConvert.DeserializeObject<Dictionary<String, String>>(result);
                var fileAudioDialogue =
                    await _repository.FindOneByConditionAsync<FileAudioDialogue>(item =>
                        item.DialogueId == Guid.Parse(dialogueId));
                //1 - InProgress
                fileAudioDialogue.StatusId = 1;
                fileAudioDialogue.TransactionId = deserializedResult["GoogleTransactionId"];
                _repository.Update(fileAudioDialogue);
                _repository.Save();
            }
        }
    }
}