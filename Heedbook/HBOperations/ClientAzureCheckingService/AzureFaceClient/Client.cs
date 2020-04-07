
using System.Collections.Generic;
using System.IO;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

namespace ClientAzureCheckingService
{
    public class AzureClient
    {
        private readonly FaceClient _client;
        private readonly AzureFaceClientSettings _settings;

        public AzureClient(AzureFaceClientSettings settings)
        {
            _client = new FaceClient(
                new ApiKeyServiceClientCredentials(settings.FaceSubscriptionKey),
                new System.Net.Http.DelegatingHandler[] { });
            _client.Endpoint = settings.FaceEndpoint;
        }

        public async System.Threading.Tasks.Task<IList<DetectedFace>> DetectGenderAgeAsync(Stream stream)
        {
            IList<FaceAttributeType> faceAttributes = new FaceAttributeType[]
            {
                FaceAttributeType.Gender, FaceAttributeType.Age
            };

            IList<DetectedFace> faceList = await _client.Face.DetectWithStreamAsync(stream, true, false, faceAttributes);
            return faceList;
        }
    }

}