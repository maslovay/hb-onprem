using System;
using RabbitMqEventBus.Base;

namespace RabbitMqEventBus.Events
{
    public class ClientAzureCheckingRun : IntegrationEvent
    {
        public String Path { get; set; }
    }
}