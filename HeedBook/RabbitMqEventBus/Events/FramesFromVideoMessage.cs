using System;
using RabbitMqEventBus.Base;

namespace RabbitMqEventBus.Events
{
    public class FramesFromVideoMessage: IntegrationEvent
    {
        public String Path { get; set; }
    }
}