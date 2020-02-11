using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

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
        [JsonIgnore] public ICollection<PhraseCompany> PhraseCompanys { get; set; }
        [JsonIgnore] public ICollection<DialoguePhrase> DialoguePhrases { get; set; }
        [JsonIgnore] public ICollection<SalesStagePhrase> SalesStagePhrases { get; set; }
    }
}