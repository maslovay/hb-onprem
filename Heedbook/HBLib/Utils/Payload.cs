using Newtonsoft.Json;

namespace HBLib.Utils
{
    public class Payload
    {
        [JsonProperty("text")]
        public string Text { get; set; }
    }
}