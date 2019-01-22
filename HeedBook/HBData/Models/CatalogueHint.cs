using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HBData.Models
{
    public class CatalogueHint
    {
        [Key]
        public Guid CatalogueHintId { get; set; }
        public string HintCondition { get; set; }
        public string HintText { get; set; }

    }
}
