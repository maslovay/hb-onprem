using System;
using System.IO;
using System.Net.Sockets;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace RabbitMqEventBus.Base
{
    public class DefaultRabbitMqPersistentConnection : IRabbitMqPersistentConnection
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly Int32 _retryCount;
        private IConnection _connection;
        private Boolean _disposed;

        private readonly Object sync_root = new Object();

        public DefaultRabbitMqPersistentConnection(IConnectionFactory connectionFactory, Int32 retryCount = 5)
        {
            _connectionFactory = connectionFactory;
            _retryCount = retryCount;
        }

        public Boolean IsConnected => _connection != null && _connection.IsOpen && !_disposed;


        public IModel CreateModel()
        {
            if (!IsConnected)
                throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");

            return _connection.CreateModel();
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            try
            {
                _connection.Dispose();
            }
            catch (IOException ex)
            {
                throw ex;
            }
        }


        public Boolean TryConnect()
        {
            lock (sync_root)
            {
                var policy = Policy.Handle<SocketException>()
                                   .Or<BrokerUnreachableException>()
                                   .WaitAndRetry(_retryCount,
                                        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                                        (ex, time) => { }
                                    );

                policy.Execute(() =>
                {
                    _connection = _connectionFactory
                       .CreateConnection();
                });

                if (IsConnected)
                {
                    _connection.ConnectionShutdown += OnConnectionShutdown;
                    _connection.CallbackException += OnCallbackException;
                    _connection.ConnectionBlocked += OnConnectionBlocked;

                    return true;
                }

                return false;
            }
        }

        private void OnConnectionBlocked(Object sender, ConnectionBlockedEventArgs e)
        {
            if (_disposed) return;
            TryConnect();
        }

        private void OnCallbackException(Object sender, CallbackExceptionEventArgs e)
        {
            if (_disposed) return;
            TryConnect();
        }

        private void OnConnectionShutdown(Object sender, ShutdownEventArgs reason)
        {
            if (_disposed) return;
            TryConnect();
        }
    }
}