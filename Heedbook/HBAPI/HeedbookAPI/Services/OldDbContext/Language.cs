using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Old.Models
{
    /// <summary>
    ///     Information about langage
    /// </summary>
    public class Language
    {
        /// <summary>
        ///     Id
        /// </summary>
        [Key]
        public Int32 LanguageId { get; set; }

        /// <summary>
        ///     Language name
        /// </summary>
        public String LanguageName { get; set; }

        /// <summary>
        ///     Language local name
        /// </summary>
        public String LanguageLocalName { get; set; }

        /// <summary>
        ///     Language short name
        /// </summary>
        public String LanguageShortName { get; set; }

        /// <summary>
        ///     Links
        /// </summary>
        public ICollection<Company> Company { get; set; }

        public ICollection<Dialogue> Dialogue { get; set; }
    }
}