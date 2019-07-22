using System.Collections.Generic;
using Newtonsoft.Json;

namespace HBLib.Utils
{
    public class Payload
    {
        [JsonProperty("attachments")]
        public List<Attachment> Attachments { get; set; }
    }
}