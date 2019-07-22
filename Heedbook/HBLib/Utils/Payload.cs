using System.Collections.Generic;
using Newtonsoft.Json;

namespace HBLib.Utils
{
    public class Payload
    {
        [JsonProperty("attechment")]
        public List<Attachment> Attachments { get; set; }
    }
}