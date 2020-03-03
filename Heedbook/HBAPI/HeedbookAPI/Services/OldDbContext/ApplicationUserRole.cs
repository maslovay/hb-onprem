using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace Old.Models
{
    public class ApplicationUserRole : IdentityUserRole<Guid>
    {
        [JsonIgnore] [ForeignKey("UserId")] public ApplicationUser User { get; set; }

        [JsonIgnore] [ForeignKey("RoleId")] public ApplicationRole Role { get; set; }
    }
}