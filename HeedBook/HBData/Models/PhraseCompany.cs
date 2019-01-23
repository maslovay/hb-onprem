using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    public class PhraseCompany
    {
        [Key]
        public int PhraseCompanyId { get; set; }

        //phrase 
        public int? PhraseId { get; set; }
        public  Phrase Phrase { get; set; }

        //Company 
        public Guid? CompanyId { get; set; }
        public  Company Company { get; set; }

    }
}
