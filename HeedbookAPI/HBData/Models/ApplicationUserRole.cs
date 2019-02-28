using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace HBData.Models
{
    public class ApplicationUserRole : IdentityUserRole<string>
    {
        public  ApplicationUser User { get; set; }
        public  ApplicationRole Role { get; set; }
    }
}