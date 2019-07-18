namespace RabbitMqEventBus.Base
{
    public class IntegrationEvent
    {
        public int RetryCount { get; set; }
        public ulong DeliveryTag { get; set; }
    }
}