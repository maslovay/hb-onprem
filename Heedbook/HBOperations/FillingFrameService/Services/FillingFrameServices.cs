using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using HBData.Models;
using HBLib;
using HBLib.Utils;
using HBMLHttpClient.Model;
using Newtonsoft.Json;
using RabbitMqEventBus.Events;

namespace FillingFrameService.Services
{
    public class FillingFrameServices
    {
        private readonly SftpClient _sftpClient;
        private readonly SftpSettings _sftpSettings;
        private readonly FFMpegWrapper _wrapper;

        public FillingFrameServices(SftpClient sftpClient,
            SftpSettings sftpSettings,
            FFMpegWrapper wrapper)
        {
            _sftpClient = sftpClient;
            _sftpSettings = sftpSettings;
            _wrapper = wrapper;
        }

        public List<DialogueFrame> FillingDialogueFrame(DialogueCreationRun message, List<FrameEmotion> emotions)
        {
            var dialogueFrames = emotions.Select(item => new DialogueFrame
            {
                DialogueId = message.DialogueId,
                AngerShare = item.AngerShare,
                FearShare = item.FearShare,
                DisgustShare = item.DisgustShare,
                ContemptShare = item.ContemptShare,
                NeutralShare = item.NeutralShare,
                SadnessShare = item.SadnessShare,
                SurpriseShare = item.SurpriseShare,
                HappinessShare = item.HappinessShare,
                YawShare = item.YawShare,
                Time = item.FileFrame.Time
            })
            .ToList();

            return dialogueFrames;
        }

        public DialogueVisual FiilingDialogueVisuals(DialogueCreationRun message, List<FrameEmotion> emotions)
        {
            var yawShare = emotions.Average(item => item.YawShare);
            yawShare = Math.Abs((double) yawShare);

            var dialogueVisual = new DialogueVisual
            {
                DialogueId = message.DialogueId,
                AngerShare = emotions.Average(item => item.AngerShare),
                FearShare = emotions.Average(item => item.FearShare),
                DisgustShare = emotions.Average(item => item.DisgustShare),
                ContemptShare = emotions.Average(item => item.ContemptShare),
                NeutralShare = emotions.Average(item => item.NeutralShare),
                SadnessShare = emotions.Average(item => item.SadnessShare),
                SurpriseShare = emotions.Average(item => item.SurpriseShare),
                HappinessShare = emotions.Average(item => item.HappinessShare),
                AttentionShare = 10 * (10 - Math.Min((double)yawShare, 10) / 1.4)
            };
            return dialogueVisual;
        }   

        public DialogueClientProfile FillingDialogueClientProfile(DialogueCreationRun message, List<FrameAttribute> attributes)
        {
            string gender; 
            if (message.Gender == null)
            {
                var genderMaleCount = attributes.Count(item => item.Gender.ToLower() == "male");
                var genderFemaleCount = attributes.Count(item => item.Gender.ToLower() == "female");
                gender = genderMaleCount > genderFemaleCount ? "Male" : "Female";
            }
            else
            {
                gender = message.Gender.ToLower();
            }

            var dialogueClientProfile = new DialogueClientProfile
            {
                DialogueId = message.DialogueId,
                Gender = gender,
                Age = attributes.Average(item => item.Age),
                Avatar = $"{message.DialogueId}.jpg"
            };

            return dialogueClientProfile;
        }  

        public async System.Threading.Tasks.Task FillingAvatarAsync(DialogueCreationRun message,
            List<FileFrame> frames, FileVideo fileVideo, bool isExtended, FileFrame fileAvatar)
        {
            
            string localPath;
            if (isExtended)
            {
                localPath =
                    await _sftpClient.DownloadFromFtpToLocalDiskAsync("frames/" + fileAvatar.FileName);
                System.Console.WriteLine($"Avatar path - {localPath}");
            

                var faceRectangle = JsonConvert.DeserializeObject<FaceRectangle>(fileAvatar.FrameAttribute.FirstOrDefault().Value);
                var rectangle = new Rectangle
                {
                    Height = faceRectangle.Height,
                    Width = faceRectangle.Width,
                    X = faceRectangle.Top,
                    Y = faceRectangle.Left
                };

                var stream = FaceDetection.CreateAvatar(localPath, rectangle);
                stream.Seek(0, SeekOrigin.Begin);
                await _sftpClient.UploadAsMemoryStreamAsync(stream, "clientavatars/", $"{message.DialogueId}.jpg");
                stream.Close();
            }
            else
            {
                if (message.ClientId != null)
                {
                    localPath =
                        await _sftpClient.DownloadFromFtpToLocalDiskAsync($"clientavatars/{message.ClientId}.jpg");
                    await _sftpClient.UploadAsync(localPath, "clientavatars/", $"{message.DialogueId}.jpg");
                }
                else
                {
                    var dt = fileAvatar.Time;
                    var seconds = dt.Subtract(fileVideo.BegTime).TotalSeconds;
                    System.Console.WriteLine($"Seconds - {seconds}, FileVideo - {fileVideo.FileName}");

                    var localVidePath =
                        await _sftpClient.DownloadFromFtpToLocalDiskAsync("videos/" + fileVideo.FileName);
                    localPath = Path.Combine(_sftpSettings.DownloadPath, fileAvatar.FileName);
                    System.Console.WriteLine($"Avatar path - {localPath}");
                    var output = await _wrapper.GetFrameNSeconds(localVidePath, localPath, Convert.ToInt32(seconds));
                    System.Console.WriteLine(output);

                    var faceRectangle = JsonConvert.DeserializeObject<FaceRectangle>(fileAvatar.FrameAttribute.FirstOrDefault().Value);
                    var rectangle = new Rectangle
                    {
                        Height = faceRectangle.Height,
                        Width = faceRectangle.Width,
                        X = faceRectangle.Top,
                        Y = faceRectangle.Left
                    };

                    var stream = FaceDetection.CreateAvatar(localPath, rectangle);
                    stream.Seek(0, SeekOrigin.Begin);
                    await _sftpClient.UploadAsMemoryStreamAsync(stream, "clientavatars/", $"{message.DialogueId}.jpg");
                    stream.Close();
                }
            }
        }

    }
}