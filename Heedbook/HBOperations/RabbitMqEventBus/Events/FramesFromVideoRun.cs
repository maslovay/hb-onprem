using System;
using RabbitMqEventBus.Base;

namespace RabbitMqEventBus.Events
{
    public class FramesFromVideoRun : IntegrationEvent
    {
        public String Path { get; set; }
        public Guid deviceId { get; set; }
    }
}