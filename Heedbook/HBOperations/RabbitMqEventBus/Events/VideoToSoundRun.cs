using System;
using RabbitMqEventBus.Base;

namespace RabbitMqEventBus.Events
{
    public class VideoToSoundRun2 : IntegrationEvent
    {
        public String Path { get; set; }
    }
}