using System;
using RabbitMqEventBus.Base;

namespace RabbitMqEventBus.Events
{
    public class DialogueCreationMessage: IntegrationEvent
    {
        public Guid ApplicationUserId { get; set; }

        public Guid DialogueId { get; set; }
        
        public DateTime BeginTime { get; set; }

        public DateTime EndTime { get; set; }
    }
}