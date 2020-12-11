using System;

namespace ApiPerformance.Models
{
    public class PhrasePost
    {
        public string PhraseText;
        public Guid PhraseTypeId;
        public Int32? LanguageId;
        public Guid SalesStageId;
        public Int32? WordsSpace;
        public double? Accurancy;
        public Boolean IsTemplate;
    }
}