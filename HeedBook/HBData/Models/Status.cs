using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HBData.Models
{
    public class Status
    {
        [Key]
        public int StatusId { get; set; }

        //название статуса
        [Required]
        public string StatusName { get; set; }

        //links
        //сотрудники статуса
        public  ICollection<ApplicationUser> ApplicationUser { get; set; }

        //диалоги статуса
        public  ICollection<Dialogue> Dialogue { get; set; }

    }
}
