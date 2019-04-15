using System;
using RabbitMqEventBus.Base;

namespace RabbitMqEventBus.Events
{
    public class ToneAnalyzeRun : IntegrationEvent
    {
        public String Path { get; set; }
    }
}