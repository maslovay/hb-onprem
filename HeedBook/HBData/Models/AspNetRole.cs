using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    /// <summary>
    /// Application user role
    /// </summary>
    public class AspNetRole
    {
        [Key]
        public Guid AspNetRoleId { get; set; }
        
        public string ConcurrencyStamp { get; set; }

        public string Name { get; set; }

        public string NormalizedName { get; set; }
    }
}
