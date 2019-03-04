using HBData.Models;
using HBData.Repository;
using HBLib.Utils;
using HBMLHttpClient;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

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

                var byteArray = await File.ReadAllBytesAsync(localPath);
                var base64String = Convert.ToBase64String(byteArray);

                var faceResult = await _client.GetFaceResult(base64String);
                var fileName = localPath.Split('/').Last();
                var fileFrame =
                    await _repository
                       .FindOneByConditionAsync<FileFrame>(entity => entity.FileName == fileName);

                if (fileFrame != null)
                {
                    var frameEmotion = new FrameEmotion
                    {
                        FileFrameId = fileFrame.FileFrameId,
                        AngerShare = faceResult.Average(item => item.Emotions.Anger),
                        ContemptShare = faceResult.Average(item => item.Emotions.Contempt),
                        DisgustShare = faceResult.Average(item => item.Emotions.Disgust),
                        FearShare = faceResult.Average(item => item.Emotions.Fear),
                        HappinessShare = faceResult.Average(item => item.Emotions.Happiness),
                        NeutralShare = faceResult.Average(item => item.Emotions.Neutral),
                        SadnessShare = faceResult.Average(item => item.Emotions.Sadness),
                        SurpriseShare = faceResult.Average(item => item.Emotions.Surprise),
                        YawShare = faceResult.Average(item => item.Headpose.Yaw)
                    };
                    var tasks = faceResult.Select(item => new FrameAttribute
                    {
                        Age = item.Attributes.Age,
                        Gender = item.Attributes.Gender,
                        Descriptor = JsonConvert.SerializeObject(item.Descriptor),
                        FileFrameId = fileFrame.FileFrameId,
                        Value = JsonConvert.SerializeObject(item.Rectangle),
                    }).Select(item => _repository.CreateAsync(item))
                        .ToList();

                    tasks.Add(_repository.CreateAsync(frameEmotion));

                    await Task.WhenAll(
                        tasks);
                    await _repository.SaveAsync();
                }
            }
        }
    }
}
