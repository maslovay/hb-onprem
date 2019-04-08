using System;
using Newtonsoft.Json;

namespace HBLib.Utils
{
    public class GoogleTransactionId
    {
        [JsonProperty("name")]
        public Int64 Name { get; set; }
    }
}