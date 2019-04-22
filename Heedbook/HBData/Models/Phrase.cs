using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    /// <summary>
    ///     Information about phrase
    /// </summary>
    public class Phrase
    {
        /// <summary>
        ///     Id
        /// </summary>
        [Key]
        public Guid PhraseId { get; set; }

        /// <summary>
        ///     Phrase text
        /// </summary>
        public String PhraseText { get; set; }

        /// <summary>
        ///     Phrase type
        /// </summary>
        public Guid? PhraseTypeId { get; set; }

        public PhraseType PhraseType { get; set; }

        /// <summary>
        ///     Phrase language
        /// </summary>
        public Int32? LanguageId { get; set; }

        public Language Language { get; set; }

        /// <summary>
        ///     Is client's phrase or employee
        /// </summary>
        public Boolean IsClient { get; set; }

        /// <summary>
        ///     Number of additional words in phrase
        /// </summary>
        public Int32? WordsSpace { get; set; }

        /// <summary>
        ///     Minimum percent of words in phrase must be detected
        /// </summary>
        public Double? Accurancy { get; set; }

        /// <summary>
        ///     Template
        /// </summary>
        public Boolean IsTemplate { get; set; }
        /// <summary>
        ///     Links
        /// </summary>
        public ICollection<PhraseCompany> PhraseCompany { get; set; }
    }
}