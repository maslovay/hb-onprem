using System.Collections.Generic;

namespace UserOperations.Models.Get.AnalyticSpeechController
{
    public class SpeechPhraseTotalInfo
    {
        public List<SpeechPhrasesInfo> Client { get; set; }
        public List<SpeechPhrasesInfo> Employee { get; set; }
        public List<SpeechPhrasesInfo> Total{ get; set; }
    }
}