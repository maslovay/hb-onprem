using DlibDotNet;
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
                if (IsFaceDetected(localPath))
                {
                    var byteArray = await File.ReadAllBytesAsync(localPath);
                    var base64String = Convert.ToBase64String(byteArray);
                    var emotions = await _client.CreateFaceEmotion(base64String);

                    var attributes = await _client.CreateFaceAttributes(base64String);

                    var fileName = localPath.Split('/').Last();

                    var fileFrame =
                        await _repository
                           .FindOneByConditionAsync<FileFrame>(entity => entity.FileName == fileName);

                    if (fileFrame != null)
                    {
                        var frameEmotion = new FrameEmotion
                        {
                            FileFrameId = fileFrame.FileFrameId,
                            AngerShare = emotions.Sum(emotion => emotion.Anger),
                            ContemptShare = emotions.Sum(emotion => emotion.Contempt),
                            DisgustShare = emotions.Sum(emotion => emotion.Disgust),
                            FearShare = emotions.Sum(emotion => emotion.Fear),
                            HappinessShare = emotions.Sum(emotion => emotion.Fear),
                            NeutralShare = emotions.Sum(emotion => emotion.Neutral),
                            SadnessShare = emotions.Sum(emotion => emotion.Sadness),
                            SurpriseShare = emotions.Sum(emotion => emotion.Sadness)
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