using System;
using System.ComponentModel.DataAnnotations;

namespace Old.Models
{
    /// <summary>
    ///     Parameters of hints for dialogues
    /// </summary>
    public class CatalogueHint
    {
        /// <summary>
        ///     Hint id
        /// </summary>
        [Key]
        public Guid CatalogueHintId { get; set; }

        /// <summary>
        ///     Special conditions for hint. Format of hint conditions is
        ///     [{"indexes": [], "isPositive": true, "max": 100, "min": 50, "operation": "sum", "table": "DialogueVisuals", "type":
        ///     "Service"}]
        /// </summary>
        public String HintCondition { get; set; }

        /// <summary>
        ///     Hint text
        /// </summary>
        public String HintText { get; set; }
    }
}