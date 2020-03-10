using System;
using System.Collections.Generic;
using RabbitMqEventBus.Base;

namespace RabbitMqEventBus.Events
{
    public class FillSlideShowDialogueRun : IntegrationEvent
    {
        public Guid DialogueId {get; set; }
    }
}