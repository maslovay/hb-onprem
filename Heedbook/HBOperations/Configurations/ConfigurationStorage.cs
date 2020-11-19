using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Base;
using Notifications.Services;
using RabbitMqEventBus;
using RabbitMqEventBus.Base;
using RabbitMQ.Client;

namespace Configurations
{
    public static class ConfigurationStorage
    {
        public static void AddRabbitMqEventBus(this IServiceCollection services,
            IConfiguration configuration)
        {
            services
               .AddSingleton<IEventBusSubscriptionsManager, InMemoryEventBusSubscriptionManager>()
               .AddSingleton<IRabbitMqPersistentConnection, DefaultRabbitMqPersistentConnection>(sp =>
                {
                    var rabbitmqsection = configuration.GetSection("RabbitMqConnection");
                    Int32.TryParse(rabbitmqsection.GetSection("Port").Value, out var port);
                    var factory = new ConnectionFactory
                    {
                        UserName = rabbitmqsection.GetSection("UserName").Value,
                        Password = rabbitmqsection.GetSection("Password").Value,
                        Port = port,
                        VirtualHost = rabbitmqsection.GetSection("VHost").Value,
                        HostName = rabbitmqsection.GetSection("HostName").Value
                    };
                    //we can set retry count into appsettings. Now defaults 5. 
                    return new DefaultRabbitMqPersistentConnection(factory);
                })
               .AddSingleton<INotificationPublisher, NotificationPublisher>(sp =>
                {
                    var rabbitMqPersistentConnection = sp.GetRequiredService<IRabbitMqPersistentConnection>();
                    var subsManager = sp.GetRequiredService<IEventBusSubscriptionsManager>();
                    var provider = sp.GetRequiredService<IServiceProvider>();
                    return new NotificationPublisher(rabbitMqPersistentConnection, subsManager, provider);
                })
               .AddSingleton<INotificationService, NotificationService>()
               .AddSingleton<INotificationHandler, NotificationHandler>();
        }
        public static void AddRabbitMqEventBusConfigFromEnv(this IServiceCollection services)
        {
            services
               .AddSingleton<IEventBusSubscriptionsManager, InMemoryEventBusSubscriptionManager>()
               .AddSingleton<IRabbitMqPersistentConnection, DefaultRabbitMqPersistentConnection>(sp =>
                {
                    var factory = new ConnectionFactory
                    {
                        UserName = Environment.GetEnvironmentVariable("RABBITMQ_CONNECTION_USERNAME"),
                        Password = Environment.GetEnvironmentVariable("RABBITMQ_CONNECTION_PASSWORD"),
                        Port = Int32.Parse(Environment.GetEnvironmentVariable("RABBITMQ_CONNECTION_PORT")),
                        VirtualHost = Environment.GetEnvironmentVariable("RABBITMQ_CONNECTION_VHOST"),
                        HostName = Environment.GetEnvironmentVariable("RABBITMQ_CONNECTION_HOSTNAME")
                    };
                    //we can set retry count into appsettings. Now defaults 5. 
                    return new DefaultRabbitMqPersistentConnection(factory);
                })
               .AddSingleton<INotificationPublisher, NotificationPublisher>(sp =>
                {
                    var rabbitMqPersistentConnection = sp.GetRequiredService<IRabbitMqPersistentConnection>();
                    var subsManager = sp.GetRequiredService<IEventBusSubscriptionsManager>();
                    var provider = sp.GetRequiredService<IServiceProvider>();
                    return new NotificationPublisher(rabbitMqPersistentConnection, subsManager, provider);
                })
               .AddSingleton<INotificationService, NotificationService>()
               .AddSingleton<INotificationHandler, NotificationHandler>();
        }
    }
}