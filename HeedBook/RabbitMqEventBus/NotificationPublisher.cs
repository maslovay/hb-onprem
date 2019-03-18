using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMqEventBus.Base;

namespace RabbitMqEventBus
{
    public class NotificationPublisher : INotificationPublisher
    {
        const string BROKER_NAME = "Notifications";
        const string DELIVERY_COUNT_HEADER = "x-delivery-count";
        private IRabbitMqPersistentConnection _persistentConnection;
        private IEventBusSubscriptionsManager _subsManager;
        private IServiceProvider _serviceProvider;
        private IModel _consumerChannel;
        private readonly int _retryCount;
        private readonly int _deliveryCount;
        public NotificationPublisher(IRabbitMqPersistentConnection persistentConnection,
            IEventBusSubscriptionsManager subsManager,
            IServiceProvider serviceProvider,
            int retryCount = 5,
            int deliveryCount = 5)
        {
            _persistentConnection = persistentConnection;
            _subsManager = subsManager;
            _retryCount = retryCount;
            _serviceProvider = serviceProvider;
            _deliveryCount = deliveryCount;
        }

        public void Publish(IntegrationEvent @event)
        {

            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            var policy = RetryPolicy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                });

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
                    properties.Headers.Add(DELIVERY_COUNT_HEADER, 0);
                    channel.BasicPublish(exchange: BROKER_NAME,
                                     routingKey: eventName,
                                     mandatory: true,
                                     basicProperties: properties,
                                     body: body);
                });
            }
        }

        public void SubscribeDynamic<TH>(string eventName)
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

        private void DoInternalSubscription(string eventName)
        {
            var containsKey = _subsManager.HasSubscriptionsForEvent(eventName);
            if (!containsKey)
            {
                if (!_persistentConnection.IsConnected)
                {
                    _persistentConnection.TryConnect();
                }

                _consumerChannel = CreateConsumerChannel(eventName);
            }
        }


        public void Unsubscribe<T, TH>()
            where TH : IIntegrationEventHandler<T>
            where T : IntegrationEvent
        {
            _subsManager.RemoveSubscription<T, TH>();
        }

        public void UnsubscribeDynamic<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler
        {
            _subsManager.RemoveDynamicSubscription<TH>(eventName);
        }

        public void Dispose()
        {
            if (_consumerChannel != null)
            {
                _consumerChannel.Dispose();
            }

            _subsManager.Clear();
        }

        private IModel CreateConsumerChannel(String eventName)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            var channel = _persistentConnection.CreateModel();

            channel.ExchangeDeclare(exchange: BROKER_NAME,
                                 type: "direct");


            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                var @event = ea.RoutingKey;
                var message = Encoding.UTF8.GetString(ea.Body);
                if(ea.BasicProperties.Headers.TryGetValue(DELIVERY_COUNT_HEADER, out var deliveryCount)){
                    var count = Int32.Parse(deliveryCount.ToString());
                    if(count >= _deliveryCount)
                    {
                        channel.BasicAck(ea.DeliveryTag, multiple: false);
                    }
                    else
                    {
                        ea.BasicProperties.Headers[DELIVERY_COUNT_HEADER] = count + 1;
                    }
                }
                await ProcessEvent(@event, message);
                
                channel.BasicAck(ea.DeliveryTag, multiple: false);
            };
            
            channel.QueueDeclare(queue: eventName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            
            channel.QueueBind(queue: eventName,
                exchange: BROKER_NAME,
                routingKey: eventName);
            
            channel.BasicConsume(queue: eventName,
                                 autoAck: false,
                                 consumer: consumer);

            channel.CallbackException += (sender, ea) =>
            {
                _consumerChannel.Dispose();
                _consumerChannel = CreateConsumerChannel(eventName);
            };
            return channel;
        }


        private async Task ProcessEvent(string eventName, string message)
        {
            if (_subsManager.HasSubscriptionsForEvent(eventName))
            {
                var subscriptions = _subsManager.GetHandlersForEvent(eventName);
                foreach (var subscription in subscriptions)
                {
                    if (subscription.IsDynamic)
                    {
                        var handler = _serviceProvider.GetService(subscription.HandlerType) as IDynamicIntegrationEventHandler;
                        dynamic eventData = JsonConvert.DeserializeObject(message);
                        handler.Handle(eventData);
                    }
                    else
                    {
                        var eventType = _subsManager.GetEventTypeByName(eventName);
                        var integrationEvent = JsonConvert.DeserializeObject(message, eventType);
                        var handler = _serviceProvider.GetService(subscription.HandlerType);
                        var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                        await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { integrationEvent });
                    }
                }
            }
        }
    }
}