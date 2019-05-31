using System;
using System.Threading.Tasks;
using MemoryDbEventBus.Events;
using MemoryDbEventBus.Handlers;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;

namespace AudioAnalyzeScheduler.Handler
{
    public class AudioAnalyzeSchedulerHandler : IMemoryDbEventHandler<FileAudioDialogueCreatedEvent>
    {
        private readonly CheckAudioRecognizeStatus _checkAudioRecognizeStatus;

        public AudioAnalyzeSchedulerHandler(CheckAudioRecognizeStatus checkAudioRecognizeStatus)
        {
            _checkAudioRecognizeStatus = checkAudioRecognizeStatus;
            EventStatus = EventStatus.InQueue;
        }

        public async Task Handle(FileAudioDialogueCreatedEvent @event)
        {
            try
            {
                EventStatus = await _checkAudioRecognizeStatus.Run(@event.Id);
            }
            catch (Exception ex)
            {
                EventStatus = EventStatus.Fail;
                throw;
            }
        }

        public EventStatus EventStatus { get; set; }
    }
}