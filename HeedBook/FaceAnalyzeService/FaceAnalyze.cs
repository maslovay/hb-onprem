using HBData.Models;
using HBData.Repository;
using HBLib.Utils;
using HBMLHttpClient;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FaceAnalyzeService
{
    public class FaceAnalyze
    {
        private readonly SftpClient _sftpClient;
        private readonly IGenericRepository _repository;
        private readonly HbMlHttpClient _client;

        public FaceAnalyze(
            SftpClient sftpClient,
            IServiceScopeFactory scopeFactory,
            HbMlHttpClient client)
        {
            _sftpClient = sftpClient ?? throw new ArgumentNullException(nameof(sftpClient));
            var scope = scopeFactory.CreateScope();
            _repository = scope.ServiceProvider.GetRequiredService<IGenericRepository>();
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task Run(String remotePath)
        {
            if (await _sftpClient.IsFileExistsAsync(remotePath))
            {
                var localPath = await _sftpClient.DownloadFromFtpToLocalDiskAsync(remotePath);
                if (FaceDetection.IsFaceDetected(localPath))
                {
                    var byteArray = await File.ReadAllBytesAsync(localPath);
                    var base64String = Convert.ToBase64String(byteArray);
                    
                    var emotions = await _client.CreateFaceEmotion(base64String);
                    foreach(var emotion in emotions){
                        System.Console.WriteLine(emotion.Anger);
                    }
                    var attributes = await _client.CreateFaceAttributes(base64String);
                    foreach(var attribute in attributes){
                        System.Console.WriteLine(attribute.Age);
                    }
                    var fileName = localPath.Split('/').Last();

                    var fileFrame =
                        await _repository
                           .FindOneByConditionAsync<FileFrame>(entity => entity.FileName == fileName);

                    if (fileFrame != null)
                    {
                        var emotionsCount = emotions.Count;
                        var frameEmotion = new FrameEmotion
                        {
                            FileFrameId = fileFrame.FileFrameId,
                            AngerShare = emotions.Average(emotion => emotion.Anger),
                            ContemptShare = emotions.Average(emotion => emotion.Contempt),
                            DisgustShare = emotions.Average(emotion => emotion.Disgust),
                            FearShare = emotions.Average(emotion => emotion.Fear),
                            HappinessShare = emotions.Average(emotion => emotion.Fear),
                            NeutralShare = emotions.Average(emotion => emotion.Neutral),
                            SadnessShare = emotions.Average(emotion => emotion.Sadness),
                            SurpriseShare = emotions.Average(emotion => emotion.Sadness)
                        };
                        var tasks = attributes.Select(faceAttributeResult => new FrameAttribute
                        {
                            Age = faceAttributeResult.Age,
                            Gender = faceAttributeResult.Gender,
                            FileFrameId = fileFrame.FileFrameId
                        }).Select(frameAttribute => _repository.CreateAsync(frameAttribute)).ToList();

                        tasks.Add(_repository.CreateAsync(frameEmotion));

                        await Task.WhenAll(
                            tasks);
                        await _repository.SaveAsync();
                    }
                }
            }
        }
    }
}