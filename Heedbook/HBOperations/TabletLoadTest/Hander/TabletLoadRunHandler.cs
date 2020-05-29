using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;

namespace TabletLoadTest.Hander
{
    public class TabletLoadRunHandler : IIntegrationEventHandler<TabletLoadRun>
    {
        private readonly TabletLoad _tabletLoad;

        public TabletLoadRunHandler(TabletLoad tabletLoad)
        {
            _tabletLoad = tabletLoad;
        }

        public async Task Handle(TabletLoadRun @event)
        {
            _tabletLoad.Run(@event);
        }
    }
}