using System;
using System.Collections.Generic;

namespace HBLib.Model
{
    public class GoogleSttAlternative
    {
        public String Transcript { get; set; }

        public Double Confidence { get; set; }

        public List<WordRecognized> Words { get; set; }
    }
}