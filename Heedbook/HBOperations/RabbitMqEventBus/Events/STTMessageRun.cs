using System;
using RabbitMqEventBus.Base;

namespace RabbitMqEventBus.Events
{
    public class STTMessageRun : IntegrationEvent
    {
        public String Path { get; set; }
    }
}