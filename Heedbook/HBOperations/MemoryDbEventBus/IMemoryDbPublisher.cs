using System;
using MemoryDbEventBus.Handlers;
using RabbitMqEventBus.Base;

namespace MemoryDbEventBus
{
    public interface IMemoryDbPublisher
    {
        void Publish(IMemoryDbEvent @event);

        void Subscribe<T, TH>()
            where T : IMemoryDbEvent
            where TH : IMemoryDbEventHandler<T>;

        void Unsubscribe<T, TH>()
            where TH : IMemoryDbEventHandler<T>
            where T : IMemoryDbEvent;
    }
}