using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace AsrHttpClient
{
    public class AsrHttpClient
    {
        private const String AudioRecognize = "asr/audiorecognize/";
        private readonly AsrSettings _asrSettings;

        public AsrHttpClient(AsrSettings asrSettings)
        {
            _asrSettings = asrSettings;
        }

        public async Task StartAudioRecognize(Guid dialogueId)
        {
            var path = _asrSettings.Uri.EndsWith('/')
                ? _asrSettings.Uri + AudioRecognize + dialogueId
                : _asrSettings.Uri + "/" + AudioRecognize + dialogueId;
            var uri = new Uri(path);
            var client = new HttpClient();
            System.Console.WriteLine($"Send request to url {uri}");
            var content = await client.GetAsync(uri);
            System.Console.WriteLine($"Status code is {content.StatusCode}");
            System.Console.WriteLine($"Http request content id {content.Content.ReadAsStringAsync()}");
        }
    }
}