using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

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