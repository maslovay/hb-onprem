using System;
using System.Collections.Generic;
using RabbitMqEventBus.Base;

namespace RabbitMqEventBus.Events
{
    public class PersonDetectionRun : IntegrationEvent
    {
        public List<Guid> DeviceIds {get; set; }
    }
}