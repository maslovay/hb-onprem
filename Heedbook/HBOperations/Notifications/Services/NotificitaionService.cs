using System;
using Notifications.Base;
using RabbitMqEventBus;
using RabbitMqEventBus.Base;

namespace Notifications.Services
{
    public class NotificationService : INotificationService
    {
        public NotificationService(INotificationPublisher publisher, INotificationHandler handler)
        {
            notificationPublisher = publisher;
            handler.EventHandlerModified += Publish;
        }

        private INotificationPublisher notificationPublisher { get; }

        public void Publish(Object sender, IntegrationEvent e)
        {
            notificationPublisher.Publish(e);
        }
    }
}