using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Polly;
using RabbitMqEventBus.Base;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace RabbitMqEventBus
{
    public class NotificationPublisher : INotificationPublisher
    {
        private const String BROKER_NAME = "Notifications";
        private const String DELIVERY_COUNT_HEADER = "x-delivery-count";
        private readonly Int32 _deliveryCount;
        private readonly IRabbitMqPersistentConnection _persistentConnection;
        private readonly Int32 _retryCount;
        private readonly IServiceProvider _serviceProvider;
        private readonly IEventBusSubscriptionsManager _subsManager;
        private IModel _consumerChannel;

        public NotificationPublisher(IRabbitMqPersistentConnection persistentConnection,
            IEventBusSubscriptionsManager subsManager,
            IServiceProvider serviceProvider,
            Int32 retryCount = 5,
            Int32 deliveryCount = 5)
        {
            _persistentConnection = persistentConnection;
            _subsManager = subsManager;
            _retryCount = retryCount;
            _serviceProvider = serviceProvider;
            _deliveryCount = deliveryCount;
            _persistentConnection.TryConnect();
        }

        public void Publish(IntegrationEvent @event)
        {
            if (!_persistentConnection.IsConnected) _persistentConnection.TryConnect();

            var policy = Policy.Handle<BrokerUnreachableException>()
                               .Or<SocketException>()
                               .WaitAndRetry(_retryCount,
                                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) => { });

            using (var channel = _persistentConnection.CreateModel())
            {
                var eventName = @event.GetType()
                                      .Name;

                var message = JsonConvert.SerializeObject(@event);
                var body = Encoding.UTF8.GetBytes(message);
                policy.Execute(() =>
                {
                    var properties = channel.CreateBasicProperties();
                    properties.DeliveryMode = 2; // persistent
                    channel.BasicPublish(BROKER_NAME,
                        eventName,
                        true,
                        properties,
                        body);
                });
            }
        }

        public void SubscribeDynamic<TH>(String eventName)
            where TH : IDynamicIntegrationEventHandler
        {
            DoInternalSubscription(eventName);
            _subsManager.AddDynamicSubscription<TH>(eventName);
        }

        public void Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = _subsManager.GetEventKey<T>();
            DoInternalSubscription(eventName);
            _subsManager.AddSubscription<T, TH>();
        }


        public void Unsubscribe<T, TH>()
            where TH : IIntegrationEventHandler<T>
            where T : IntegrationEvent
        {
            _subsManager.RemoveSubscription<T, TH>();
        }

        public void UnsubscribeDynamic<TH>(String eventName)
            where TH : IDynamicIntegrationEventHandler
        {
            _subsManager.RemoveDynamicSubscription<TH>(eventName);
        }

        private void DoInternalSubscription(String eventName)
        {
            var containsKey = _subsManager.HasSubscriptionsForEvent(eventName);
            if (!containsKey)
            {
                if (!_persistentConnection.IsConnected) _persistentConnection.TryConnect();

                _consumerChannel = CreateConsumerChannel(eventName);
            }
        }

        public void Dispose()
        {
            if (_consumerChannel != null) _consumerChannel.Dispose();

            _subsManager.Clear();
        }

        private IModel CreateConsumerChannel(String eventName)
        {
            if (!_persistentConnection.IsConnected) _persistentConnection.TryConnect();

            var channel = _persistentConnection.CreateModel();

            channel.ExchangeDeclare(BROKER_NAME,
                "direct");


            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var @event = ea.RoutingKey;
                    var message = Encoding.UTF8.GetString(ea?.Body ?? new byte[0]);
                    var eventMessage = ((IntegrationEvent)JsonConvert.DeserializeObject(message, _subsManager.GetEventTypeByName(@event)));
                    if (eventMessage.RetryCount >= _deliveryCount)
                    {
                        Console.WriteLine($"Message deleted from queue after {_deliveryCount} retries");
                        channel.BasicAck(ea.DeliveryTag, false);
                    }
                    else
                    {
                        await ProcessEvent(@event, message);
                        channel.BasicAck(ea.DeliveryTag, false);
                    }
                }
                catch (Exception e)
                {
                    System.Console.WriteLine($"Exception:\n{e}");
                    var encodingString = Encoding.UTF8.GetString(ea?.Body ?? new byte[0]);
                    var @event = (IntegrationEvent)JsonConvert.DeserializeObject(encodingString,
                        _subsManager.GetEventTypeByName(ea.RoutingKey));
                    Console.WriteLine(@event.RetryCount);
                    @event.RetryCount += 1;
                    Console.WriteLine("exception occured in rabbitmq event bus, retry count is: " + @event.RetryCount);
                    channel.BasicAck(ea.DeliveryTag, false);
                    Publish(@event);
                }
            };

            channel.QueueDeclare(eventName,
                true,
                false,
                false,
                null);

            channel.QueueBind(eventName,
                BROKER_NAME,
                eventName);

            channel.BasicConsume(eventName,
                false,
                consumer);
            
            channel.CallbackException += (sender, ea) =>
            {
                _consumerChannel.Dispose();
                _consumerChannel = CreateConsumerChannel(eventName);
            };
            return channel;
        }


        private async Task ProcessEvent(String eventName, String message)
        {
            if (_subsManager.HasSubscriptionsForEvent(eventName))
            {
                var subscriptions = _subsManager.GetHandlersForEvent(eventName);
                foreach (var subscription in subscriptions)
                    if (subscription.IsDynamic)
                    {
                        var handler =
                            _serviceProvider.GetService(subscription.HandlerType) as IDynamicIntegrationEventHandler;
                        dynamic eventData = JsonConvert.DeserializeObject(message);
                        handler.Handle(eventData);
                    }
                    else
                    {
                        var eventType = _subsManager.GetEventTypeByName(eventName);
                        var integrationEvent = JsonConvert.DeserializeObject(message, eventType);
                        var handler = _serviceProvider.GetService(subscription.HandlerType);
                        var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                        await (Task) concreteType.GetMethod("Handle").Invoke(handler, new[] {integrationEvent});
                    }
            }
        }
    }
}