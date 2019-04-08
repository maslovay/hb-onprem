using System;
using System.Collections.Generic;
using HBData.Models;

namespace HBLib.Utils
{
    public static class Tables
    {
        public static readonly Dictionary<String, Type> TablesDictionary;

        static Tables()
        {
            TablesDictionary = new Dictionary<String, Type>()
            {
                {"DialogueVisuals", typeof(DialogueVisual)},
                {"DialogueAudios", typeof(DialogueAudio)},
                {"DialogueSpeechs", typeof(DialogueSpeech)},
                {"DialogueClientSatisfactions", typeof(DialogueClientSatisfaction)},
                {"DialoguePhraseCounts", typeof(DialoguePhraseCount)}
            };
        }
    }
}