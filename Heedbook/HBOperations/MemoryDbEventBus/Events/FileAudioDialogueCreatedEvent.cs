using System;
using RabbitMqEventBus.Base;

namespace MemoryDbEventBus.Events
{
    public class FileAudioDialogueCreatedEvent : MemoryDbEvent
    {
        public int Status { get; set; }
        public new string EventType { get; private set; } = "FileAudioDialogueCreatedEvent";
    }
}
