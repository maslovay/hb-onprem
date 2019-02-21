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
                //var token = await _googleConnector.GetAuthorizationToken("./");
                var token = await _googleConnector.GetAuthorizationToken(currentPath);
                
                var blobGoogleDriveName =
                        dialogueId + "_client" + Path.GetExtension(fileName);
                await _googleConnector.LoadFileToGoogleDrive(blobGoogleDriveName, path, token);
                await _googleConnector.MakeFilePublicGoogleCloud(blobGoogleDriveName, "./", token);
                await _googleConnector.Recognize(blobGoogleDriveName, languageId, dialogueId, true, true);
            }
            Console.WriteLine("Function Audio STT finished");
        }
        
    }
}