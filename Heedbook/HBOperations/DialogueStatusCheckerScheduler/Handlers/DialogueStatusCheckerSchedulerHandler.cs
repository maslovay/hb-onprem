using System;
using System.Threading.Tasks;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;
using MemoryDbEventBus;
using MemoryDbEventBus.Events;
using MemoryDbEventBus.Handlers;

namespace DialogueStatusCheckerScheduler.Handler
{
    public class DialogueStatusCheckerScheduler : IMemoryDbEventHandler<DialogueCreatedEvent>
    {
        private readonly DialogueStatusChecker _dialogueStatusChecker;

        public DialogueStatusCheckerScheduler(DialogueStatusChecker dialogueStatusChecker)
        {
            _dialogueStatusChecker = dialogueStatusChecker;
            EventStatus = EventStatus.InQueue;
        }

        public async Task Handle(DialogueCreatedEvent @event)
        {
            try
            {
                await _dialogueStatusChecker.Run(@event.Id);
            }
            catch (Exception ex)
            {
                EventStatus = EventStatus.Fail;
                throw;
            }

            EventStatus = EventStatus.Passed;
        }

        public EventStatus EventStatus { get; set; }
    }
}