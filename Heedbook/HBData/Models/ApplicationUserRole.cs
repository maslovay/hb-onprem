using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;


namespace HBData.Models
{
    public class ApplicationUserRole : IdentityUserRole<Guid>
    {
        [ForeignKey("UserId")]
        public  ApplicationUser User { get; set; }
        [ForeignKey("RoleId")]
        public  ApplicationRole Role { get; set; }
    }
}