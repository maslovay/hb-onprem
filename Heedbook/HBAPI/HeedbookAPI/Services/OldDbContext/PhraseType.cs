using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Old.Models
{
    public class PhraseType
    {
        [Key] public Guid PhraseTypeId { get; set; }

        //phrase type text
        public String PhraseTypeText { get; set; }

        //colour for charts
        public String Colour { get; set; }

        //colour for charts
        public String ColourSyn { get; set; }

        //links
        //phrases
        public ICollection<Phrase> Phrase { get; set; }
    }
}