using System;
using RabbitMqEventBus.Base;

namespace RabbitMqEventBus.Events
{
    public class MessengerMessageRun : IntegrationEvent
    {
        public String logText { get; set; }
        public String ChannelName { get; set; }
    }
}
