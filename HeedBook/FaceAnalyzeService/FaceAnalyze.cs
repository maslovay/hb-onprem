using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DlibDotNet;
using FaceAnalyzeService.Model;
using HBData.Models;
using HBData.Repository;
using HBLib.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using RabbitMqEventBus.Events;

namespace FaceAnalyzeService
{
    public class FaceAnalyze
    {
        private readonly ILogger<FaceAnalyze> _logger;
        private readonly SftpClient _sftpClient;
        private readonly IGenericRepository _repository;

        public FaceAnalyze(
            ILogger<FaceAnalyze> logger,
            SftpClient sftpClient,
            IGenericRepository repository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sftpClient = sftpClient ?? throw new ArgumentNullException(nameof(sftpClient));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task Run(String remotePath)
        {
            if (await _sftpClient.IsFileExistsAsync(remotePath))
            {
                var localPath = await _sftpClient.DownloadFromFtpToLocalDiskAsync(remotePath);
                if (IsFaceDetected(localPath))
                {
                    var random = new Random();
                    var emotion = new FaceEmotionResult
                    {
                        Fear = (Single) random.NextDouble(),
                        Anger = (Single) random.NextDouble(),
                        Contempt = (Single) random.NextDouble(),
                        Disgust = (Single) random.NextDouble(),
                        Happiness = (Single) random.NextDouble(),
                        Neutral = (Single) random.NextDouble(),
                        Sadness = (Single) random.NextDouble(),
                        Surprise = (Single) random.NextDouble()
                    };

                    var attributes = new FaceAttributeResult
                    {
                        Age = (Byte) random.Next(),
                        Gender = "male"
                    };
                    var fileName = localPath.Split('/').Last();

                    var fileFrame =
                        await _repository
                           .FindOneByConditionAsync<FileFrame>(entity => entity.FileName == fileName);

                    if (fileFrame != null)
                    {
                        var frameEmotion = new FrameEmotion
                        {
                            FileFrameId = fileFrame.FileFrameId,
                            AngerShare = emotion.Anger,
                            ContemptShare = emotion.Contempt,
                            DisgustShare = emotion.Disgust,
                            FearShare = emotion.Fear,
                            HappinessShare = emotion.Happiness,
                            NeutralShare = emotion.Neutral,
                            SadnessShare = emotion.Sadness,
                            SurpriseShare = emotion.Surprise,
                            YawShare = random.NextDouble()
                        };

                        var frameAttribute = new FrameAttribute
                        {
                            Age = attributes.Age,
                            Gender = attributes.Gender,
                            FileFrameId = fileFrame.FileFrameId
                        };
                        Task.WaitAll(
                            _repository.CreateAsync(frameEmotion),
                            _repository.CreateAsync(frameAttribute));
                    }
                }
            }
        }

        private Boolean IsFaceDetected(String localPath)
        {
            using (var detector = Dlib.GetFrontalFaceDetector())
            {
                using (var img = Dlib.LoadImage<Byte>(localPath))
                {
                    Dlib.PyramidUp(img);
                    var detectionResult = detector.Operator(img).Length;
                    return detectionResult > 0;
                }
            }
        }
    }
}