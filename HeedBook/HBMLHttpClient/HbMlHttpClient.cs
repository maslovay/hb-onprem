using HBMLHttpClient.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace HBMLHttpClient
{
    public class HbMlHttpClient
    {
        private readonly HttpSettings _hbMlSettings;

        private const String FaceAttributesPath = "face_attributes/";

        private const String FaceEmotionsPath = "face_emotions/";

        public HbMlHttpClient(HttpSettings hbMlSettings)
        {
            _hbMlSettings = hbMlSettings;
        }

        public async Task<List<FaceAttributeResult>> CreateFaceAttributes(String base64StringFile)
        {
            var path = _hbMlSettings.HbMlUri.EndsWith('/')
                ? _hbMlSettings.HbMlUri + FaceAttributesPath
                : _hbMlSettings.HbMlUri + "/" + FaceAttributesPath;
            var uri = new Uri(path);
            var client = new HttpClient();
            var content = new StringContent(base64StringFile);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            var response = await client.PostAsync(uri, content);
            var contentAsString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<FaceAttributeResult>>(contentAsString);
        }

        public async Task<List<FaceEmotionResult>> CreateFaceEmotion(string base64StringFile)
        {
            var path = _hbMlSettings.HbMlUri.EndsWith('/') 
                ? _hbMlSettings.HbMlUri + FaceEmotionsPath 
                : _hbMlSettings.HbMlUri + "/" + FaceEmotionsPath;
            var uri = new Uri(path);
            var client = new HttpClient();
            var content = new StringContent(base64StringFile);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            var response = await client.PostAsync(uri, content);
            var contentAsString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<FaceEmotionResult>>(contentAsString);
        }
    }
}
