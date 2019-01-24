using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    /// <summary>
    /// Information about company phrases
    /// </summary>
    public class PhraseCompany
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        public Guid PhraseCompanyId { get; set; }
        /// <summary>
        /// Phrase id
        /// </summary>
        public Guid? PhraseId { get; set; }
        public  Phrase Phrase { get; set; }
        /// <summary>
        /// Company id
        /// </summary>
        public Guid? CompanyId { get; set; }
        public  Company Company { get; set; }

    }
}
