using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostgreSQL.Models
{
    public class Phrase
    {
        public int PhraseId { get; set; }

        //text of phrase
        public string PhraseText { get; set; }

        //phrase type
        public int? PhraseTypeId { get; set; }
        public  PhraseType PhraseType { get; set; }

        //language
        public int? LanguageId { get; set; }
        public  Language Language { get; set; }


        //client phrase
        public bool IsClient { get; set; }

        //Words Space
        public int? WordsSpace { get; set; }

        //accurancy
        public double? Accurancy { get; set; }

        //Template
        public bool Template { get; set; }


        //links
        //phrases of company
        public  ICollection<PhraseCompany> PhraseCompany { get; set; }
    }
}
