using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HBData.Models
{
    /// <summary>
    /// Information about langage
    /// </summary>
    public class Language
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        public int LanguageId { get; set; }
        /// <summary>
        /// Language name
        /// </summary>
        public string LanguageName { get; set; }
        /// <summary>
        /// Language local name
        /// </summary>
        public string LanguageLocalName { get; set; }
        /// <summary>
        /// Language short name 
        /// </summary>
        public string LanguageShortName { get; set; }
        /// <summary>
        /// Links
        /// </summary>
        public  ICollection<Company> Company { get; set; }
        public  ICollection<Dialogue> Dialogue { get; set; }
    }
}
