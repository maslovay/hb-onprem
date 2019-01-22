using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HBData.Models
{
    public class Country
    {
        [Key]
        public int CountryId { get; set; }

        //название страны
        public string CountryName { get; set; }

        //links
        //компании языка
        public  ICollection<Company> Company { get; set; }
        
    }
}
