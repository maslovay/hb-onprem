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
        /// <summary>
        /// Application user role id
        /// </summary>
        [Key]
        public Guid AspNetRoleId { get; set; }
        
        public string ConcurrencyStamp { get; set; }

        /// <summary>
        /// User name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Normalized user name
        /// </summary>
        public string NormalizedName { get; set; }
    }
}
