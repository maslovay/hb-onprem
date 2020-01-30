using System;
using RabbitMqEventBus.Base;

namespace RabbitMqEventBus.Events
{
    public class DialogueCreationRun : IntegrationEvent
    {
        public Guid DeviceId { get; set; }
        public Guid? ApplicationUserId { get; set; }
        public Guid DialogueId { get; set; }
        public DateTime BeginTime { get; set; }
        public DateTime EndTime { get; set; }
        public string AvatarFileName {get;set;}
        public string Gender {get;set;}
        public Guid? ClientId {get;set;}
    }
}