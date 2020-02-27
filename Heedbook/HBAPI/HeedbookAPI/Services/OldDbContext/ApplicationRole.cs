using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace Old.Models
{
    public class ApplicationRole : IdentityRole<Guid>
    {
        [JsonIgnore] public ICollection<ApplicationUserRole> UserRoles { get; set; }
    }
}