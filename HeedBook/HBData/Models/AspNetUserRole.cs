using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBData.Models
{
    public class AspNetUserRole
    {
        [Key]
        public Guid AspNetUserRoleId { get; set; }

        public string RoleId { get; set; }
    }
}
