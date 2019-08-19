using System.Collections.Generic;

namespace HBLib
{
    public class AzureSettings
    {
        public string TenantId { get; set; }

        public string ClientId { get; set; } 
        
        public string Secret { get; set; }

        public string ResourceGroup { get; set; }
        public List<string> VmNames { get; set; }
        
        public List<string> Metrics { get; set; }

        
    }
}