using System;
using RabbitMqEventBus.Base;

namespace MemoryDbEventBus.Events
{
    public class DialogueCreatedEvent : MemoryDbEvent
    {
        public int Status { get; set; }
        public new string EventType { get; private set; } = "DialogueCreatedEvent";
    }
}