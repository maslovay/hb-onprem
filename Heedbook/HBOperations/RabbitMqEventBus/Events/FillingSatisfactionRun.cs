using System;
using System.Collections.Generic;
using System.Text;
using RabbitMqEventBus.Base;

namespace RabbitMqEventBus.Events
{
    public class FillingSatisfactionRun: IntegrationEvent
    {
        public Guid DialogueId { get; set; }
    }
}
