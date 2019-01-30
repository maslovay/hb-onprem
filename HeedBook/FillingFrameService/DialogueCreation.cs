using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using RabbitMqEventBus.Events;

namespace FillingFrameService
{
    public class DialogueCreation
    {
        private readonly IGenericRepository _repository;

        public DialogueCreation(IGenericRepository repository)
        {
            _repository = repository;
        }

        public async Task Run(DialogueCreationMessage message)
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

            await Task.WhenAll(insertTasks);
            await _repository.SaveAsync();
        }
    }
}