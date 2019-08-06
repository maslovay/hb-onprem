using Newtonsoft.Json;

namespace HBLib.Utils
{
    public class Field
    {
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; }
        [JsonProperty("short")]
        public bool Short { get; set; }
        
        [JsonProperty("functionname")]
        public string FucnctionName { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; }
        
        public int TikTak { get; set; }
        
    }
}