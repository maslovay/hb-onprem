using System;
using RabbitMqEventBus.Base;

namespace MemoryDbEventBus.Events
{
    public class DialogueCreatedEvent : IMemoryDbEvent
    {
        public Guid Id { get; set; }
        public int Status { get; set; }
    }
}