using System;
using RabbitMqEventBus.Base;

namespace RabbitMqEventBus.Events
{
    public class VideoToSoundRun: IntegrationEvent
    {
        public String Path { get; set; }
    }
}
