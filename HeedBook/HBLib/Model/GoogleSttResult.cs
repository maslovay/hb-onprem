using System;
using System.Collections.Generic;

namespace HBLib.Model
{
    public class GoogleSttResult
    {
        public List<WordRecognised> Words { get; set; }
    }

    public class WordRecognised
    {
        public DateTime BegTime { get; set; }

        public DateTime EndTime { get; set; }

        public String Word { get; set; }
    }
}