﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AsrHttpClient
{
    public class AsrHttpClient
    {
        private readonly AsrSettings _asrSettings;

        private const String AudioRecognize = "asr/audiorecognize/";

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