using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HBLib.Models
{
    public class Country
    {
        public int CountryId { get; set; }

        //название страны
        public string CountryName { get; set; }

        //links
        //компании языка
        public virtual ICollection<Company> Company { get; set; }
        
    }
}
