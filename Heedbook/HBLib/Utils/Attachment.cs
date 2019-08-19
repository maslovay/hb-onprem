using System.Collections.Generic;
using System.Drawing;
using ErrorKibanaScheduler;
using Newtonsoft.Json;

namespace HBLib.Utils
{
    public class Attachment
    {
        [JsonProperty("color")]
        public  string Color { get; set; }
        [JsonProperty("pretext")]
        public string Pretext { get; set; }
        [JsonProperty("fields")]
        public List<Field> Fields { get; set; }
        [JsonProperty("text")]
        public string Text { get; set;}
        [JsonProperty("author_name")]
        public string AuthorName { get; set;}
        [JsonProperty("title")]
        public string Title { get; set; }
    }
}
