using System;
using RabbitMqEventBus.Base;

namespace RabbitMqEventBus.Events
{
    public class VideoContentToGifRun : IntegrationEvent
    {
        public String Path { get; set; }
    }
}