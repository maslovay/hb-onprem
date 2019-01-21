using RabbitMqEventBus.Base;
using RabbitMqEventBus.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqEventBus.Events
{
    public class FaceRecognitionRun: IntegrationEvent
    {
        public FaceRecognitionMessage Data { get; set; }
    }
}
