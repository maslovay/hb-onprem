using System;
using RabbitMqEventBus.Base;

namespace RabbitMqEventBus.Events
{
    public class FillingHintsRun: IntegrationEvent
    {
        public Guid DialogueId { get; set; }
    }
}