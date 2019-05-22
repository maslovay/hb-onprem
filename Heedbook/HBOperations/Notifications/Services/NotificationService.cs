using System;
using System.Collections.Generic;
using Notifications.Base;
using RabbitMqEventBus;
using RabbitMqEventBus.Base;

namespace Notifications.Services
{
    public class NotificationService : INotificationService
    {
        private Queue<IntegrationEvent> events = new Queue<IntegrationEvent>();
        
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