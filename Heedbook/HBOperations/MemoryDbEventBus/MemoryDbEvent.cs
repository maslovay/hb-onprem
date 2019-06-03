using System;

namespace MemoryDbEventBus
{
    public class MemoryDbEvent : IMemoryDbEvent
    {
        public Guid Id { get; set; }
        public string EventType { get; private set; } = "MemoryDbEvent";
    }
}