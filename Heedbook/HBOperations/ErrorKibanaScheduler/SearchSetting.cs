using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ErrorKibanaScheduler
{
    public class SearchSetting
    {
        public string host;

        public double port;

        public string[] tags;

        public string type;

        [JsonProperty("@timestamp")] 
        public DateTime Timestamp { get; set; }

        public int version;
        
        [JsonProperty("FunctionName")]
        public string FunctionName { get; set; }

        [JsonProperty("LogLevel")]
        public string LogLevel { get; set; }
        
        [JsonProperty("OriginalFormat")]
        public string OriginalFormat { get; set; }

        public string InvocationId;
        
        public int TikTak { get; set; }
        
        public string CustomDimensions;
        
    }
}