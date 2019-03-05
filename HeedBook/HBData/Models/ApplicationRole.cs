using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace HBData.Models
{
    public class ApplicationRole : IdentityRole<Guid>
    {
        public ICollection<ApplicationUserRole> UserRoles { get; set; }
    }
}