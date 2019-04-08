using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace HBLib.Model
{
    public class GoogleSttResult
    {
        public List<GoogleSttAlternative> Alternatives { get; set; }
    }
}