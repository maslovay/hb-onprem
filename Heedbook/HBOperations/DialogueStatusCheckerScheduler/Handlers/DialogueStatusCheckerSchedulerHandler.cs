using System;
using System.Threading.Tasks;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;
using MemoryDbEventBus;
using MemoryDbEventBus.Events;
using MemoryDbEventBus.Handlers;

namespace DialogueStatusCheckerScheduler.Handler
{
    public class DialogueStatusCheckerSchedulerHandler : IMemoryDbEventHandler<DialogueCreatedEvent>
    {
        private readonly DialogueStatusChecker _dialogueStatusChecker;

        public DialogueStatusCheckerSchedulerHandler(DialogueStatusChecker dialogueStatusChecker)
        {
            _dialogueStatusChecker = dialogueStatusChecker;
            EventStatus = EventStatus.InQueue;
        }

        public async Task Handle(DialogueCreatedEvent @event)
        {
            try
            {
                EventStatus = await _dialogueStatusChecker.Run(@event.Id);
            }
            catch (Exception ex)
            {
                EventStatus = EventStatus.Fail;
                throw;
            }
        }

        public EventStatus EventStatus { get; set; }
    }
}