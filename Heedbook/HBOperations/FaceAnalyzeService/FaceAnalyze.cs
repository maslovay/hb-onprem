using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FaceAnalyzeService.Exceptions;
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
                _log.Info($"{remotePath}");
                if (await _sftpClient.IsFileExistsAsync(remotePath))
                {
                    var localPath = await _sftpClient.DownloadFromFtpToLocalDiskAsync(remotePath);
                    _log.Info($"Download to path - {localPath}");


                    var buffer = File.ReadAllBytes(localPath);
                    _log.Info($"File size in bytes -- {buffer.ToArray().Count()}");

                    FaceDetection.IsFaceDetected(localPath, out var faceLength1);
                    _log.Info($"Is face detected method1 -- {faceLength1}");
                    FaceDetection.IsFaceDetected(buffer.ToArray(), out var faceLength2);
                    _log.Info($"Is face detected method2 -- {faceLength2}");
         

                    if (FaceDetection.IsFaceDetected(localPath, out var faceLength))
                    {
                        _log.Info("Face detected!");

                        var byteArray = await File.ReadAllBytesAsync(localPath);
                        var base64String = Convert.ToBase64String(byteArray);

                        var faceResult = await _client.GetFaceResult(base64String);
                        _log.Info($"Face result is {JsonConvert.SerializeObject(faceResult)}");
                        var fileName = localPath.Split('/').Last();
                        var fileFrame = await _repository
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
                            _log.Info("Fileframe not null. Calculate average and insert frame emotion and frame attribute");
                            await Task.WhenAll(tasks);
                            await _repository.SaveAsync();
                        }
                    }
                    else
                    {
                        _log.Info("No face detected!");
                    }
                    File.Delete(remotePath);
                }
                else
                {
                    _log.Info($"No such file {remotePath}");
                }
                _log.Info("Function face analyze finished");

            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occured {e}");
                throw new FaceAnalyzeServiceException(e.Message, e);
            }
        }
    }
}