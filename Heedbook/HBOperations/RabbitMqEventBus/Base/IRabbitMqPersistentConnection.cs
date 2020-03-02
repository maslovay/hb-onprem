using System;
using RabbitMQ.Client;

namespace RabbitMqEventBus.Base
{
    public interface IRabbitMqPersistentConnection : IDisposable
    {
        Boolean IsConnected { get; }

        Boolean TryConnect();

        IModel CreateModel();
    }
}