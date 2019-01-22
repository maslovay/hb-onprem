using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostgreSQL.Models
{
    public class PhraseType
    {
        public int PhraseTypeId { get; set; }

        //phrase type text
        public string PhraseTypeText { get; set; }

        //colour for charts
        public string Colour { get; set; }

        //colour for charts
        public string ColourSyn { get; set; }

        //links
        //phrases
        public  ICollection<Phrase> Phrase { get; set; }
    }
}
