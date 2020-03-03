using System;
using RabbitMqEventBus.Base;

namespace RabbitMqEventBus.Events
{
    public class FillingSatisfactionRun : IntegrationEvent
    {
        public Guid DialogueId { get; set; }
    }
}