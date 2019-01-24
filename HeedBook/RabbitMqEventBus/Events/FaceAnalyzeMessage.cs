using System;
using RabbitMqEventBus.Base;

namespace RabbitMqEventBus.Events
{
    public class FaceAnalyzeMessage: IntegrationEvent
    {
        public String Path { get; set; }
    }
}