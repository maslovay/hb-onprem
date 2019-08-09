using System.Collections.Generic;
using ErrorKibanaScheduler;
using Newtonsoft.Json;

namespace HBLib.Utils
{
    public class Payload : Message
    {
        [JsonProperty("attachments")]
        public List<Attachment> Attachments { get; set; }
        
    }
}