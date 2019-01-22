using System.Threading.Tasks;
using CognitiveService.Legacy;
using Microsoft.Azure.WebJobs;
using Notifications.Base;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;
using RabbitMqEventBus.Models;

namespace CognitiveService.Handlers
{
    public class FaceRecognitionMessageHandler : IIntegrationEventHandler<FaceRecognitionRun>
    {
        private readonly FrameSubFaceReq _faceReq;

        public FaceRecognitionMessageHandler(FrameSubFaceReq faceReq)
        {
            _faceReq = faceReq;
        }

        public async Task Handle(FaceRecognitionRun @event)
        {
            await _faceReq.Run(@event.Data, new ExecutionContext());
        }
    }
}