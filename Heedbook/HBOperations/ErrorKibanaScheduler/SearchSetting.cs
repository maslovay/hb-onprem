using System;
using Newtonsoft.Json;

namespace ErrorKibanaScheduler
{
    public class SearchSetting
    {
        public string host;

        public double port;

        public string[] tags;

        public string type;

        [JsonProperty("@timestamp")] public string Timestamp { get; set; }

        public int version;

        public string FunctionName;

        public string LogLevel;

        public string OriginalFormat;

        public string InvocationId;

        public string CustomDimensions;
    }
}