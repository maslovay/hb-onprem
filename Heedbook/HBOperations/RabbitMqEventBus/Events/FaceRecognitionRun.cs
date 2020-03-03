using RabbitMqEventBus.Base;
using RabbitMqEventBus.Models;

namespace RabbitMqEventBus.Events
{
    public class FaceRecognitionRun : IntegrationEvent
    {
        public FaceRecognitionMessage Data { get; set; }
    }
}