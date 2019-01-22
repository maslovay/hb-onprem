using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HBData.Models
{
    public class DialogueHint
    {
        [Key]
        public int DialogueHintId { get; set; }

        //dialogue 
        public Guid? DialogueId { get; set; }
        public Dialogue Dialogue { get; set; }

        //hint text
        public string HintText { get; set; }

        //is automatic created
        public bool IsAutomatic { get; set; }

        //hint type (Service, Efficiency, Cross-sales, TextAnalytics)
        public string Type { get; set; }

        //is positive recomendation
        public bool IsPositive { get; set; }
    }
}