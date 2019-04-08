using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    /// <summary>
    /// Parameters of hints for dialogues
    /// </summary>
    public class CatalogueHint
    {
        /// <summary>
        /// Hint id
        /// </summary>
        [Key]
        public Guid CatalogueHintId { get; set; }
        /// <summary>
        /// Special conditions for hint. Format of hint conditions is 
        /// [{"indexes": [], "isPositive": true, "max": 100, "min": 50, "operation": "sum", "table": "DialogueVisuals", "type": "Service"}] 
        /// </summary>
        public string HintCondition { get; set; }
        /// <summary>
        /// Hint text
        /// </summary>
        public string HintText { get; set; }

    }
}
