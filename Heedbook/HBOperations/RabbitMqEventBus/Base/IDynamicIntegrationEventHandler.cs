using System.Threading.Tasks;

namespace RabbitMqEventBus.Base
{
    public interface IDynamicIntegrationEventHandler
    {
        Task Handle(dynamic eventData);
    }
}