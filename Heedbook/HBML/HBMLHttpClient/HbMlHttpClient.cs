using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using HBMLHttpClient.Model;
using Newtonsoft.Json;

namespace HBMLHttpClient
{
    public class HbMlHttpClient
    {
        private const String FaceEmotionsPath = "face/";
        private readonly HttpSettings _hbMlSettings;

        public HbMlHttpClient(HttpSettings hbMlSettings)
        {
            _hbMlSettings = hbMlSettings;
        }

        public async Task<List<FaceResult>> GetFaceResult(String base64StringFile)
        {
            var path = _hbMlSettings.HbMlUri.EndsWith('/')
                ? _hbMlSettings.HbMlUri + FaceEmotionsPath
                : _hbMlSettings.HbMlUri + "/" + FaceEmotionsPath;
            var uri = new Uri(path);
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(5);
            var content = new StringContent(base64StringFile);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            var response = await client.PostAsync(uri, content);
            var contentAsString = await response.Content.ReadAsStringAsync();

            // to do: delete or change to elastic
            Console.WriteLine($"{contentAsString}");

            return JsonConvert.DeserializeObject<List<FaceResult>>(contentAsString);
        }
    }
}