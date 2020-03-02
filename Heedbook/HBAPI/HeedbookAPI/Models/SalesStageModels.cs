using HBData.Models;
using System;
using System.Collections.Generic;

namespace UserOperations.Models
{
    public class GetSalesStage
    {
        public Guid SalesStageId { get; set; }
        public string SalesStageName { get; set; }
        public int SalesStageNumber { get; set; }
        public List<Phrase> Phrases { get; set; }
    }
}
