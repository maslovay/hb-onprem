using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HBLib.Models
{
    public class CompanyIndustry
    {
        public int CompanyIndustryId { get; set; }

        //название страны
        public string CompanyIndustryName { get; set; }

        //links
        //компании языка
        public virtual ICollection<Company> Company { get; set; }
        
    }
}
