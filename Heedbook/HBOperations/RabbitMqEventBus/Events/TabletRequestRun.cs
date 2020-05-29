using System;
using RabbitMqEventBus.Base;

namespace RabbitMqEventBus.Events
{
    public class TabletRequestRun : IntegrationEvent
    {
        public String RequestName { get; set; }
        public Guid DeviceId { get; set; }
        public Guid CompanyId { get; set; }
        public Guid ApplicationUserId { get; set; }
    }
}
