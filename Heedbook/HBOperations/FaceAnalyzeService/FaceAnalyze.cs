using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FaceAnalyzeService.Exceptions;
using HBData;
using HBData.Models;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using HBMLHttpClient;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Renci.SshNet.Common;

namespace FaceAnalyzeService
{
    public class FaceAnalyze
    {
        private readonly HbMlHttpClient _client;
        private readonly ElasticClient _log;
        private readonly RecordsContext _context;
        private readonly SftpClient _sftpClient;
        private readonly Object _syncRoot = new Object();
        
        public FaceAnalyze(
            SftpClient sftpClient,
            IServiceScopeFactory factory,
            HbMlHttpClient client,
            // ElasticClient log
            )
        {
            _sftpClient = sftpClient ?? throw new ArgumentNullException(nameof(sftpClient));
            _context = factory.CreateScope().ServiceProvider.GetRequiredService<RecordsContext>();
            _client = client ?? throw new ArgumentNullException(nameof(client));
            // _log = log;
        }

        public async Task Run(String remotePath)
        {
            var settings = new ElasticSettings{
                Host = "13.74.249.78",
                Port = 5000,
                FunctionName = "OnPremExtractFaceAnalyzeTest"
            };
            var _log = new ElasticClient(settings);
            try
            {
                _log.Info("Function face analyze started");
                //_log.Info($"{remotePath}");
                if (await _sftpClient.IsFileExistsAsync(remotePath))
                {
                    string localPath;
                    lock (_syncRoot) {
                        localPath = _sftpClient.DownloadFromFtpToLocalDiskAsync(remotePath).GetAwaiter().GetResult();
                    }
                    // _log.Info($"Download to path - {localPath}");

                    // var fileStream = File.OpenRead(localPath);
                    // var ms = new MemoryStream();
                    // fileStream.CopyTo(ms);
                    // _log.Info($"File size in bytes -- {ms.ToArray().Count()}");

                    // FaceDetection.IsFaceDetected(localPath, out var faceLength1);
                    // _log.Info($"Is face detected method1 -- {faceLength1}");
                    // FaceDetection.IsFaceDetected(ms.ToArray(), out var faceLength2);
                    // _log.Info($"Is face detected method2 -- {faceLength2}");

                    if (FaceDetection.IsFaceDetected(localPath, out var faceLength))
                    {
                        _log.Info("Face detected!");

                        var byteArray = await File.ReadAllBytesAsync(localPath);
                        var base64String = Convert.ToBase64String(byteArray);

                        var faceResult = await _client.GetFaceResult(base64String);
                        _log.Info($"Face result is {JsonConvert.SerializeObject(faceResult)}");
                        var fileName = localPath.Split('/').Last();
                        var fileFrame = _context.FileFrames.Where(entity => entity.FileName == fileName).FirstOrDefault();

                        if (fileFrame != null && faceResult.Any())
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

                            var frameAttribute = faceResult.Select(item => new FrameAttribute
                            {
                                Age = item.Attributes.Age,
                                Gender = item.Attributes.Gender,
                                Descriptor = JsonConvert.SerializeObject(item.Descriptor),
                                FileFrameId = fileFrame.FileFrameId,
                                Value = JsonConvert.SerializeObject(item.Rectangle)
                            }).FirstOrDefault();

                            fileFrame.FaceLength = faceLength;
                            fileFrame.IsFacePresent = true;

                            if (frameAttribute != null)  _context.FrameAttributes.Add(frameAttribute);
                            _context.SaveChanges();
                        }
                    }
                    else
                    {
                        _log.Info("No face detected!");
                    }
                    _log.Info("Function finished");

                    File.Delete(remotePath);
                }
                else
                {
                    _log.Info($"No such file {remotePath}");
                }

                _log.Info("Function face analyze finished");

            }
            catch (SftpPathNotFoundException e)
            {
                _log.Fatal($"{e}");
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occured {e}");
                throw new FaceAnalyzeServiceException(e.Message, e);
            }
        }
    }
}