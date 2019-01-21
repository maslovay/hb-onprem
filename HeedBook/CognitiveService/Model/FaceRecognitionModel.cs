using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using RabbitMqEventBus.Models;

namespace CognitiveService.Model
{
    public class FaceRecognitionModel: BsonDocument
    {
        [BsonId]
        public ObjectId Id { get; set; }
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