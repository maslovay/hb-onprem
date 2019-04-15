using System;
using RabbitMqEventBus.Base;

namespace Notifications.Base
{
    public interface INotificationService
    {
        void Publish(Object sender, IntegrationEvent e);
    }
}