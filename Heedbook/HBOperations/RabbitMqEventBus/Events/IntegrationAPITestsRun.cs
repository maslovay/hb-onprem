using System;
using System.Collections.Generic;
using RabbitMqEventBus.Base;

namespace RabbitMqEventBus.Events
{
    public class IntegrationAPITestsRun : IntegrationEvent
    {
        public String Command { get; set; }
    }
}