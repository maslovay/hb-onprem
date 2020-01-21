using System;
using System.Net.Http;
using System.Threading.Tasks;
using HBLib.Model;

namespace HBLib.Utils
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
            await client.GetAsync(uri);
        }
    }
}