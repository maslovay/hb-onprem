using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace HBLib.Model
{
    public class WordRecognized
    {
        [JsonProperty("startTime")]
        public String StartTime { get; set; }

        [JsonProperty("endTime")]
        public String EndTime { get; set; }

        [JsonProperty("word")]
        public String Word { get; set; }
    }
}