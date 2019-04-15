using System;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    /// <summary>
    ///     Information about hints for dialogue
    /// </summary>
    public class DialogueHint
    {
        /// <summary>
        ///     Dilaogue hint id
        /// </summary>
        [Key]
        public Guid DialogueHintId { get; set; }

        /// <summary>
        ///     Dialogue id
        /// </summary>
        public Guid? DialogueId { get; set; }

        public Dialogue Dialogue { get; set; }

        /// <summary>
        ///     Text of hint
        /// </summary>
        public String HintText { get; set; }

        /// <summary>
        ///     Created by teacher or not
        /// </summary>
        public Boolean IsAutomatic { get; set; }

        /// <summary>
        ///     Hint type (Service, Efficiency, Cross-sales, TextAnalytics)
        /// </summary>
        public String Type { get; set; }

        /// <summary>
        ///     Is recomendation positive
        /// </summary>
        public Boolean IsPositive { get; set; }
    }
}