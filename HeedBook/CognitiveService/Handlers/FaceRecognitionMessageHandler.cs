using System.Threading.Tasks;
using CognitiveService.Legacy;
using Microsoft.Azure.WebJobs;
using Notifications.Base;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Models;

namespace CognitiveService.Handlers
{
    public class FaceRecognitionMessageHandler : IIntegrationEventHandler<FaceRecognitionMessage>
    {
        private readonly FrameSubFaceReq _faceReq;

        public FaceRecognitionMessageHandler(FrameSubFaceReq faceReq)
        {
            _faceReq = faceReq;
        }

        public async Task Handle(FaceRecognitionMessage @event)
        {
            await _faceReq.Run(@event, new ExecutionContext());
        }
    }
}