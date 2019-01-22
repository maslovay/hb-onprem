using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PostgreSQL.Models
{
    /// <summary>
    /// Application user role
    /// </summary>
    public class AspNetRole
    {
        // User role id
        public Guid AspNetRoleId { get; set; }
        
        public string ConcurrencyStamp { get; set; }

        public string Name { get; set; }

        public string NormalizedName { get; set; }
    }
}
