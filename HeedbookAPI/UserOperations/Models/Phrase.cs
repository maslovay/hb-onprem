using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace UserOperations.Models
{
    /// <summary>
    /// Information about phrase
    /// </summary>
    public class Phrase
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        public Guid PhraseId { get; set; }
        /// <summary>
        /// Phrase text
        /// </summary>
        public string PhraseText { get; set; }
        /// <summary>
        /// Phrase type
        /// </summary>
        public Guid? PhraseTypeId { get; set; }
        public  PhraseType PhraseType { get; set; }
        /// <summary>
        /// Phrase language
        /// </summary>
        public int? LanguageId { get; set; }
        public Language Language { get; set; }
        /// <summary>
        /// Is client's phrase or employee
        /// </summary>
        public bool IsClient { get; set; }
        /// <summary>
        /// Number of additional words in phrase 
        /// </summary>
        public int? WordsSpace { get; set; }
        /// <summary>
        /// Minimum percent of words in phrase must be detected
        /// </summary>
        public double? Accurancy { get; set; }
        /// <summary>
        /// Template
        /// </summary>
        public bool Template { get; set; }
        /// <summary>
        /// Links
        /// </summary>
        public  ICollection<PhraseCompany> PhraseCompany { get; set; }
    }
}
