using System;
using RabbitMqEventBus.Base;

namespace RabbitMqEventBus.Events
{
    public class FaceAnalyzeRun : IntegrationEvent
    {
        public String Path { get; set; }
    }
}