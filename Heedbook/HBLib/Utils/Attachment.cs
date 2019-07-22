using System.Collections.Generic;
using System.Drawing;
using Newtonsoft.Json;

namespace HBLib.Utils
{
    public class Attachment
    {
        [JsonProperty("color")]
        public  string Color { get; set; }
        [JsonProperty("pretext")]
        public string Pretext { get; set; }
        [JsonProperty("field")]
        public List<Field> Fields { get; set; }
    }
}