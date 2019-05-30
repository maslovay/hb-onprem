using RabbitMqEventBus.Base;

namespace MemoryDbEventBus.Handlers
{
    public enum EventStatus
    {
        InQueue,
        Passed,
        Fail
    }
    
    public interface IMemoryDbEventHandler<in T> : IIntegrationEventHandler<T>
    {
        EventStatus EventStatus { get; set; }
    }
}