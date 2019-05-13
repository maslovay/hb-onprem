using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RabbitMqEventBus;

namespace UnitTestExtensions
{
    public static class StartupExtensions
    {
        public static void MockRabbitPublisher(IServiceCollection services)
        {
            InstantiateDueToExistingServiceLifeTime(services, typeof(INotificationPublisher), typeof(RabbitPublisherMock));
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

        public static bool CheckInterfaceImplements( Type objType, Type interfaceType )
        {
            if (objType == interfaceType)
                return true;
            
            return interfaceType.IsInterface 
                   && objType.GetInterfaces().Any(i => i.GetType() == interfaceType);
        }
    }
}