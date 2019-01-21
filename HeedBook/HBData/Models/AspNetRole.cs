using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    public class AspNetRole
    {
        [Key]
        public string Id { get; set; }
        
        public string ConcurrencyStamp { get; set; }

        public string Name { get; set; }

        public string NormalizedName { get; set; }
    }
}
