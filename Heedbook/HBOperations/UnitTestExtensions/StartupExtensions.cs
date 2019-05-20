using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notifications.Base;
using RabbitMqEventBus;

namespace UnitTestExtensions
{
    public static class StartupExtensions
    {
        public static void MockRabbitPublisher(IServiceCollection services)
        {
            InstantiateDueToExistingServiceLifeTime(services, typeof(INotificationPublisher), typeof(RabbitPublisherMock));
        }
        
        public static void MockNotificationService(IServiceCollection services)
        {
            InstantiateDueToExistingServiceLifeTime(services, typeof(INotificationService), typeof(NotificationServiceMock));
        }

        public static void MockNotificationHandler(IServiceCollection services)
        {
            InstantiateDueToExistingServiceLifeTime(services, typeof(INotificationHandler), typeof(NotificationHandlerMock));
        }
        
        private static void InstantiateDueToExistingServiceLifeTime(IServiceCollection services, Type interfaceType, Type mockType)
        {
            var serviceLifeTime = ServiceLifetime.Scoped;

            var existingPublisher =
                services.FirstOrDefault(s => CheckInterfaceImplements(s.ServiceType, interfaceType));

            if (existingPublisher != null)
            {
                serviceLifeTime = existingPublisher.Lifetime;
                services.RemoveAll(interfaceType);
            }

            switch (serviceLifeTime)
            {
                case ServiceLifetime.Scoped:
                    services.AddScoped(interfaceType, mockType);
                    break;
                case ServiceLifetime.Singleton:
                    services.AddSingleton(interfaceType, mockType);
                    break;
                default:
                case ServiceLifetime.Transient:
                    services.AddTransient(interfaceType, mockType);
                    break;
            }
        }

        
        /*
                        .AddSingleton<INotificationService, NotificationService>()
               .AddSingleton<INotificationHandler, NotificationHandler>();
         */
        public static bool CheckInterfaceImplements( Type objType, Type interfaceType )
        {
            if (objType == interfaceType)
                return true;
            
            return interfaceType.IsInterface 
                   && objType.GetInterfaces().Any(i => i.GetType() == interfaceType);
        }
    }
}