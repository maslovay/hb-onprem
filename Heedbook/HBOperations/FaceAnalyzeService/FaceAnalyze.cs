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
        private readonly RecordsContext _context;
        private readonly SftpClient _sftpClient;
        private readonly Object _syncRoot = new Object();
        private readonly ElasticClientFactory _elasticClientFactory;
        public FaceAnalyze(
            SftpClient sftpClient,
            IServiceScopeFactory factory,
            HbMlHttpClient client,
            ElasticClientFactory elasticClientFactory
            )
        {
            _sftpClient = sftpClient ?? throw new ArgumentNullException(nameof(sftpClient));
            _context = factory.CreateScope().ServiceProvider.GetRequiredService<RecordsContext>();
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _elasticClientFactory = elasticClientFactory;
        }

        public void GetAll(Func<String, bool> func)
        {
        }

        public async Task Run(String remotePath)
        {
            
            var _log = _elasticClientFactory.GetElasticClient();
            _log.SetFormat("{Path}");
            _log.SetArgs(remotePath);
            
            try
            {
                _log.Info($"Function started");
                if (await _sftpClient.IsFileExistsAsync(remotePath))
                {
                    string localPath;
                    lock (_syncRoot)
                    {
                        localPath = _sftpClient.DownloadFromFtpToLocalDiskAsync(remotePath).GetAwaiter().GetResult();
                    }
                    _log.Info($"Download to path - {localPath}");
                    if (FaceDetection.IsFaceDetected(localPath, out var faceLength))
                    {
                        _log.Info($"{localPath}: Face detected!");

                        var byteArray = await File.ReadAllBytesAsync(localPath);
                        var base64String = Convert.ToBase64String(byteArray);

                        var faceResult = await _client.GetFaceResult(base64String);
                        _log.Info($"Face result is {JsonConvert.SerializeObject(faceResult)}");
                        var fileName = localPath.Split('/').Last();
                        FileFrame fileFrame;
                        lock (_context)
                        {
                            fileFrame = _context.FileFrames.Where(entity => entity.FileName == fileName).FirstOrDefault();
                        }
                        if (fileFrame != null && faceResult.Any())
                        {
                            Random rnd = new Random();
                            var disgust = Convert.ToDouble(rnd.Next(0, 100)) / 1000;
                            var contempt = Convert.ToDouble(rnd.Next(0, 100)) / 1000;
                            var fear = Convert.ToDouble(rnd.Next(0, 100)) / 1000;
                            var anger = 1 -  1 / 2 * (disgust + contempt + fear);
                            var sadness = 1 -  1 / 2 * (disgust + contempt + fear);

                            var frameEmotion = new FrameEmotion
                            {
                                FileFrameId = fileFrame.FileFrameId,
                                AngerShare = anger * faceResult.Average(item => item.Emotions.Anger),
                                // ContemptShare = faceResult.Average(item => item.Emotions.Contempt),
                                // DisgustShare = faceResult.Average(item => item.Emotions.Disgust),
                                // FearShare = faceResult.Average(item => item.Emotions.Fear),
                                HappinessShare = faceResult.Average(item => item.Emotions.Happiness),
                                NeutralShare = faceResult.Average(item => item.Emotions.Neutral),
                                SadnessShare = sadness * faceResult.Average(item => item.Emotions.Sadness),
                                SurpriseShare = faceResult.Average(item => item.Emotions.Surprise),
                                YawShare = faceResult.Average(item => item.Headpose.Yaw)
                            };
                            frameEmotion.ContemptShare = contempt * (frameEmotion.AngerShare + frameEmotion.SadnessShare);
                            frameEmotion.DisgustShare = disgust * (frameEmotion.AngerShare + frameEmotion.SadnessShare);
                            frameEmotion.FearShare = fear * (frameEmotion.AngerShare + frameEmotion.SadnessShare);

                            var sum = frameEmotion.AngerShare + frameEmotion.HappinessShare + frameEmotion.NeutralShare + frameEmotion.SadnessShare 
                                + frameEmotion.SurpriseShare + frameEmotion.ContemptShare + frameEmotion.DisgustShare + frameEmotion.FearShare;
                            
                            frameEmotion.AngerShare = frameEmotion.AngerShare / sum;
                            frameEmotion.HappinessShare = frameEmotion.HappinessShare / sum;
                            frameEmotion.NeutralShare = frameEmotion.NeutralShare / sum;
                            frameEmotion.SadnessShare = frameEmotion.SadnessShare / sum;
                            frameEmotion.SurpriseShare = frameEmotion.SurpriseShare / sum;
                            frameEmotion.ContemptShare = frameEmotion.ContemptShare / sum;
                            frameEmotion.DisgustShare = frameEmotion.DisgustShare / sum;
                            frameEmotion.FearShare = frameEmotion.FearShare / sum;

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

                            if (frameAttribute != null) _context.FrameAttributes.Add(frameAttribute);
                            _context.FrameEmotions.Add(frameEmotion);
                            lock (_context)
                            {
                                _context.SaveChanges();
                            }
                        }
                    }
                    else
                    {
                        _log.Info($"{localPath}: No face detected!");
                    }
                    _log.Info("Function finished");

                    File.Delete(localPath);
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