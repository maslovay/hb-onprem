using System;

namespace PersonDetectionAkBarsService.Models
{
    public class HeedbookSettingsInAkBars
    {
        public string ProjectId { get; set; }
        public Guid ClientId { get; set;}
        public string RegisterNewCustomerUrl { get; set; }
        public string ValidateCustomerUrl { get; set; }
    }
}