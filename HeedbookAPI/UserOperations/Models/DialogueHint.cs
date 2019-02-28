using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace UserOperations.Models
{
    /// <summary>
    /// Information about hints for dialogue 
    /// </summary>
    public class DialogueHint
    {
        /// <summary>
        /// Dilaogue hint id
        /// </summary>
        [Key]
        public Guid DialogueHintId { get; set; }
        /// <summary>
        /// Dialogue id
        /// </summary>
        public Guid? DialogueId { get; set; }
        public Dialogue Dialogue { get; set; }
        /// <summary>
        /// Text of hint
        /// </summary>
        public string HintText { get; set; }
        /// <summary>
        /// Created by teacher or not
        /// </summary>
        public bool IsAutomatic { get; set; }
        /// <summary>
        /// Hint type (Service, Efficiency, Cross-sales, TextAnalytics)
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// Is recomendation positive
        /// </summary>
        public bool IsPositive { get; set; }
    }
}