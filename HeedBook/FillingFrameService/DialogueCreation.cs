using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Microsoft.Extensions.DependencyInjection;
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
            var frameIds =
                _repository.Get<FileFrame>().Where(item =>
                                item.ApplicationUserId == message.ApplicationUserId
                                && item.Time >= message.BeginTime
                                && item.Time <= message.EndTime)
                           .Select(item => item.FileFrameId)
                           .ToList();
            var emotions =
                await _repository.FindByConditionAsync<FrameEmotion>(item => frameIds.Contains(item.FileFrameId));
            var attributes =
                await _repository.FindByConditionAsync<FrameAttribute>(item => frameIds.Contains(item.FileFrameId));
            if (emotions.Any() && attributes.Any())
            {
                var emotionsCount = emotions.Count();
                var attributesCount = attributes.Count();
                var dialogueFrames = emotions.Select(item => new DialogueFrame
                    {
                        AngerShare = item.AngerShare,
                        FearShare = item.FearShare,
                        DisgustShare = item.DisgustShare,
                        ContemptShare = item.ContemptShare,
                        NeutralShare = item.NeutralShare,
                        SadnessShare = item.SadnessShare,
                        SurpriseShare = item.SurpriseShare,
                        HappinessShare = item.HappinessShare,
                        YawShare = default(Double)
                    })
                    .ToList();

                var genderCount = attributes.Count(item => item.Gender == "male");

                var dialogueClientProfile = new DialogueClientProfile
                {
                    DialogueId = message.DialogueId,
                    Gender = genderCount > 0 ? "male" : "female",
                    Age = attributes.Sum(item => item.Age) / attributesCount,
                    Avatar = $"{message.DialogueId}.jpg"
                };

                var dialogueVisual = new DialogueVisual
                {
                    DialogueId = message.DialogueId,
                    AngerShare = emotions.Sum(item => item.AngerShare) / emotionsCount,
                    FearShare = emotions.Sum(item => item.FearShare) / emotionsCount,
                    DisgustShare = emotions.Sum(item => item.DisgustShare) / emotionsCount,
                    ContemptShare = emotions.Sum(item => item.ContemptShare) / emotionsCount,
                    NeutralShare = emotions.Sum(item => item.NeutralShare) / emotionsCount,
                    SadnessShare = emotions.Sum(item => item.SadnessShare) / emotionsCount,
                    SurpriseShare = emotions.Sum(item => item.SurpriseShare) / emotionsCount,
                    HappinessShare = emotions.Sum(item => item.HappinessShare) / emotionsCount,
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

                var stream = FaceDetection.CreateAvatar(localPath);
                stream.Seek(0, SeekOrigin.Begin);
                await _sftpClient.UploadAsMemoryStreamAsync(stream, "clientavatars/", $"{message.DialogueId}.jpg");
                stream.Close();
                await Task.WhenAll(insertTasks);
                await _repository.SaveAsync();
            }
        }
    }
}