using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HBLib.Models;

namespace HBData.Models
{
    public class Status
    {
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
