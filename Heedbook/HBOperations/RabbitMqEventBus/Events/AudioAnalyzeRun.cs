using System;
using RabbitMqEventBus.Base;

namespace RabbitMqEventBus.Events
{
    public class AudioAnalyzeRun : IntegrationEvent
    {
        public String Path { get; set; }
    }
}