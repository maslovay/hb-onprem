using System;
using LemmaSharp;

namespace HBLib.Utils
{
    public static class LemmatizerFactory
    {
        public static ILemmatizer CreateLemmatizer(Int32 languageId)
        {
            switch (languageId)
            {
                case 1:
                    return new Lemmatizer();
                case 2:
                    return new LemmatizerPrebuiltCompact(LanguagePrebuilt.Russian);
                case 3:
                    return new LemmatizerPrebuiltCompact(LanguagePrebuilt.Spanish);
                case 4:
                    return new LemmatizerPrebuiltCompact(LanguagePrebuilt.French);
                case 5:
                    return new LemmatizerPrebuiltCompact(LanguagePrebuilt.Italian);
                case 8:
                    return new LemmatizerPrebuiltCompact(LanguagePrebuilt.German);
                case 10:
                    return new LemmatizerPrebuiltCompact(LanguagePrebuilt.Hungarian);
                default:
                    return new Lemmatizer();
            }
        }
    }
}