using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AsrHttpClient
{
    public class AsrHttpClient
    {
        private readonly AsrSettings _asrSettings;

        public AsrHttpClient(AsrSettings asrSettings)
        {
            _asrSettings = asrSettings;
        }

        public async Task<List<AsrResult>> StartAudioAnalyze(String filename)
        {
            var path = _asrSettings.Uri.EndsWith('/')
                ? _asrSettings.Uri + "asr/audiorecognize/" + filename
                : _asrSettings.Uri + "/" + "asr/audiorecognize/" + filename;
            var uri = new Uri(path);
            var client = new HttpClient();
            var response = await client.GetAsync(uri);
            var contentAsString = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<List<AsrResult>>(contentAsString);
        }
    }
}