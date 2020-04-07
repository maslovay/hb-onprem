
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Newtonsoft.Json;

namespace ClientAzureCheckingService
{
    public class AzureClient
    {
        private readonly FaceClient _client;
        private readonly AzureFaceClientSettings _settings;

        public AzureClient(AzureFaceClientSettings settings)
        {
            _settings = settings;
            _client = new FaceClient(
                new ApiKeyServiceClientCredentials(settings.FaceSubscriptionKey),
                new System.Net.Http.DelegatingHandler[] { });
            _client.Endpoint = settings.FaceEndpoint;
        }

        public async System.Threading.Tasks.Task<IList<DetectedFace>> DetectGenderAgeAsync(String path)
        {
            IList<FaceAttributeType> faceAttributes = new FaceAttributeType[]
            {
                FaceAttributeType.Gender, FaceAttributeType.Age
            };
            try
            {
                var url = _settings.ImageURL + path;
                System.Console.WriteLine(url);
                IList<DetectedFace> faceList = await _client.Face.DetectWithUrlAsync(url, true, false, faceAttributes);
                System.Console.WriteLine(JsonConvert.SerializeObject(faceList));
                return faceList;
            }
            catch (APIErrorException f)
            {
                System.Console.WriteLine(f);
                return null;
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e);
                return null;
            }

        }
    }

}