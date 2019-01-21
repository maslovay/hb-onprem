using System;
using RabbitMqEventBus.Base;

namespace RabbitMqEventBus.Models  {

    public class FaceRecognitionMessage: IntegrationEvent
    {
        public String Id { get; set; }
        public Guid ApplicationUserId { get; set; }
        public DateTime Time { get; set; }
        public Byte FacesLength { get; set; }
        public Boolean IsFacePresent { get; set; }
        public String BlobName { get; set; }
        public String BlobContainer { get; set; }
        public VideoStatus Status { get; set; }
        public DateTime CreationTime { get; set; }
    }
}