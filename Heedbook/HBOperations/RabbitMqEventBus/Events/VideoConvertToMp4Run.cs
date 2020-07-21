using System;
using RabbitMqEventBus.Base;

namespace RabbitMqEventBus.Events
{
    public class VideoConvertToMp4Run : IntegrationEvent
    {
        public String DialogueId { get; set; }
    }
}