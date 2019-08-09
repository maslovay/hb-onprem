using HBLib;
using Newtonsoft.Json;

namespace ErrorKibanaScheduler
{
    public class TelegramMessage : Message
    {
        [JsonProperty("logText")]
        public string logText { get; set; }
    }
}