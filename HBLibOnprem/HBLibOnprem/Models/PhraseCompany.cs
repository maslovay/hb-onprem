using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HBLib.Models
{
    public class PhraseCompany
    {
        public int PhraseCompanyId { get; set; }

        //phrase 
        public int? PhraseId { get; set; }
        public virtual Phrase Phrase { get; set; }

        //Company 
        public int? CompanyId { get; set; }
        public virtual Company Company { get; set; }

    }
}
