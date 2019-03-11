using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using HBMLHttpClient.Model;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RabbitMqEventBus.Events;

namespace FillingFrameService
{
    public class DialogueCreation
    {
        private readonly IGenericRepository _repository;
        private readonly SftpClient _sftpClient;
        private readonly SftpSettings _sftpSettings;
        public DialogueCreation(IServiceScopeFactory factory,
            SftpClient client,
            SftpSettings sftpSettings)
        {
            var scope = factory.CreateScope();
            _repository = scope.ServiceProvider.GetRequiredService<IGenericRepository>();
            _sftpClient = client;
            _sftpSettings = sftpSettings;
        }

        public async Task Run(DialogueCreationRun message)
        {
            System.Console.WriteLine("Function started");
            var frameIds =
                _repository.Get<FileFrame>().Where(item =>
                                item.ApplicationUserId == message.ApplicationUserId
                                && item.Time >= message.BeginTime
                                && item.Time <= message.EndTime)
                           .Select(item => item.FileFrameId)
                           .ToList();
            var emotions =
                _repository.GetWithInclude<FrameEmotion>(item => frameIds.Contains(item.FileFrameId), item => item.FileFrame).ToList();
            var attributes =
                await _repository.FindByConditionAsync<FrameAttribute>(item => frameIds.Contains(item.FileFrameId));
            if (emotions.Any() && attributes.Any())
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
                        YawShare = default(Double),
                        Time = item.FileFrame.Time
                    })
                    .ToList();

                var genderCount = attributes.Count(item => item.Gender == "Male");

                var dialogueClientProfile = new DialogueClientProfile
                {
                    DialogueId = message.DialogueId,
                    Gender = genderCount > 0 ? "male" : "female",
                    Age = attributes.Average(item => item.Age),
                    Avatar = $"{message.DialogueId}.jpg"
                };

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
                    AttentionShare = default(Double)
                };

                var insertTasks = new List<Task>
                {
                    _repository.CreateAsync(dialogueVisual),
                    _repository.CreateAsync(dialogueClientProfile),
                    _repository.BulkInsertAsync(dialogueFrames)
                };

                var localPath =
                    await _sftpClient.DownloadFromFtpToLocalDiskAsync("frames/" + emotions.First().FileFrame.FileName);
                var attribute = attributes.First(item => item.FileFrameId == emotions.First(emotion => frameIds.Contains(emotion.FileFrameId)).FileFrameId);

                var faceRectangle = JsonConvert.DeserializeObject<FaceRectangle>(attribute.Value);
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
                await Task.WhenAll(insertTasks);
                await _repository.SaveAsync();
                System.Console.WriteLine("Function finished");
            }
        }
    }
}