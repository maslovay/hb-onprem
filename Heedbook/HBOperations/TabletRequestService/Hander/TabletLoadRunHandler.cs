using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;

namespace TabletRequestService.Hander
{
    public class TabletRequestRunHandler : IIntegrationEventHandler<TabletRequestRun>
    {
        private readonly TabletRequestService _tabletRequestService;

        public TabletRequestRunHandler(TabletRequestService tabletRequestService)
        {
            _tabletRequestService = tabletRequestService;
        }

        public async Task Handle(TabletRequestRun @event)
        {
            _tabletRequestService.Run(@event);
        }
    }
}