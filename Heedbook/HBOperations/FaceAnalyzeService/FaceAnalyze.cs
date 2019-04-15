using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using HBLib.Utils;
using HBMLHttpClient;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace FaceAnalyzeService
{
    public class FaceAnalyze
    {
        private readonly HbMlHttpClient _client;
        private readonly ElasticClient _log;
        private readonly IGenericRepository _repository;
        private readonly SftpClient _sftpClient;

        public FaceAnalyze(
            SftpClient sftpClient,
            IServiceScopeFactory factory,
            HbMlHttpClient client,
            ElasticClient log)
        {
            _sftpClient = sftpClient ?? throw new ArgumentNullException(nameof(sftpClient));
            _repository = factory.CreateScope().ServiceProvider.GetRequiredService<IGenericRepository>();
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _log = log;
        }

        public async Task Run(String remotePath)
        {
            try
            {
                _log.Info("Function face analyze started");
                if (await _sftpClient.IsFileExistsAsync(remotePath))
                {
                    var localPath = await _sftpClient.DownloadFromFtpToLocalDiskAsync(remotePath);

                    if (FaceDetection.IsFaceDetected(localPath, out var faceLength))
                    {
                        _log.Info("face detected!");

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
                                                       Value = JsonConvert.SerializeObject(item.Rectangle)
                                                   }).Select(item => _repository.CreateAsync(item))
                                                  .ToList();

                            fileFrame.FaceLength = faceLength;
                            fileFrame.IsFacePresent = true;
                            _repository.Update(fileFrame);
                            tasks.Add(_repository.CreateAsync(frameEmotion));
                            _log.Info(
                                "fileframe not null. Calculate average and insert frame emotion and frame attribute");
                            await Task.WhenAll(
                                tasks);
                            await _repository.SaveAsync();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occured {e}");
                throw;
            }
        }
    }
}