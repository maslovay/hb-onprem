using System;

namespace UserOperations.Models.Get.AnalyticSpeechController
{
    public class PhrasesInfo
    {
        public Boolean IsClient;
        public string FullName;
        public Guid? ApplicationUserId;
        public Guid? DialogueId;
        public Guid? PhraseId;
        public string PhraseText;
        public string PhraseTypeText;
    }
}