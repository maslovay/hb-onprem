using System;
using System.Collections.Generic;
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
        private readonly ElasticClient _log;
        public FaceAnalyze(
            SftpClient sftpClient,
            IServiceScopeFactory factory,
            HbMlHttpClient client,
            ElasticClient log,
            RecordsContext context
            )
        {
            _sftpClient = sftpClient ?? throw new ArgumentNullException(nameof(sftpClient));
            _context = context;
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _log = log;
        }

        public void GetAll(Func<String, bool> func)
        {
        }

        public async Task Run(String remotePath)
        {
            _log.SetFormat("{Path}");
            _log.SetArgs(remotePath);
            HelthTime.Time = DateTime.Now;
            try
            {
                _log.Info($"Function started");
                string localPath;
                lock (_syncRoot)
                {
                    localPath = _sftpClient.DownloadFromFtpToLocalDiskAsync(remotePath).GetAwaiter().GetResult();
                }
                _log.Info($"Download to path - {localPath}");
                if (FaceDetection.IsFaceDetected(localPath, out var faceLength))
                {
                    _log.Info($"{localPath}: Face detected!  {faceLength}: faceLength");

                    var byteArray = await File.ReadAllBytesAsync(localPath);
                    var base64String = Convert.ToBase64String(byteArray);
                    var faceResult = await _client.GetFaceResult(base64String);
                    _log.Info($"Face result is {JsonConvert.SerializeObject(faceResult)}");
                    
                    var fileName = localPath.Split('/').Last();
                    var fileFrame = _context.FileFrames.Where(entity => entity.FileName == fileName).FirstOrDefault();
                    
                    var frameEmotion = new List<FrameEmotion>();
                    var frameAttribute = new List<FrameAttribute>();
                    if (fileFrame != null && faceResult.Any())
                    {
                        fileFrame.FaceLength = faceLength;
                        fileFrame.IsFacePresent = true;
                        frameEmotion.Add(new FrameEmotion
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
                        });
                        
                        frameAttribute.Add(faceResult.Select(item => new FrameAttribute
                        {
                            Age = item.Attributes.Age,
                            Gender = item.Attributes.Gender,
                            Descriptor = JsonConvert.SerializeObject(item.Descriptor),
                            FileFrameId = fileFrame.FileFrameId,
                            Value = JsonConvert.SerializeObject(item.Rectangle)
                        }).FirstOrDefault());

                        if (frameAttribute.Any()) 
                        {
                            _log.Info($"Saving frame attributes {JsonConvert.SerializeObject(frameAttribute)}");
                            _context.FrameAttributes.AddRange(frameAttribute);
                        }
                        if (frameEmotion.Any()) 
                        {
                            _log.Info($"Saving frame emotions {JsonConvert.SerializeObject(frameEmotion)}");
                            _context.FrameEmotions.AddRange(frameEmotion);
                        }
                        _context.SaveChanges();
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