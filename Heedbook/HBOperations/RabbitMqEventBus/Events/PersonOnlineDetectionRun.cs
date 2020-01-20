using System;
using RabbitMqEventBus.Base;

namespace RabbitMqEventBus.Events
{
    public class PersonOnlineDetectionRun : IntegrationEvent
    {
        public String Path { get; set; }
        public String Descriptor {get; set;}
        public Guid? CompanyId {get; set;}
        public int Age {get;set;}
        public string Gender{get;set;}
        public Guid CorporationId {get;set;}
        public Guid? DeviceId {get;set;}
    }
}