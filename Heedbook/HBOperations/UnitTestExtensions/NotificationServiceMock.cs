using Notifications.Base;
using Notifications.Services;
using RabbitMqEventBus;
using RabbitMqEventBus.Base;

namespace UnitTestExtensions
{
    public class NotificationServiceMock : NotificationService
    {
        public NotificationServiceMock(INotificationPublisher publisher, INotificationHandler handler) : base(publisher, handler)
        {
            
        }
    }
}