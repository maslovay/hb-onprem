using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HBLib.Models
{
    public class Status
    {
        public int StatusId { get; set; }

        //название статуса
        [Required]
        public string StatusName { get; set; }

        //links
        //сотрудники статуса
        public virtual ICollection<ApplicationUser> ApplicationUser { get; set; }

        //диалоги статуса
        public virtual ICollection<Dialogue> Dialogue { get; set; }

    }
}
