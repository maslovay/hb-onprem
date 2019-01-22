using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PostgreSQL.Models
{
    public class CompanyIndustry
    {
        public int CompanyIndustryId { get; set; }

        //название страны
        public string CompanyIndustryName { get; set; }

        public double? SatisfactionIndex { get; set; }
        public double? LoadIndex { get; set; }
        public double? CrossSalesIndex { get; set; }

        //links
        //компании языка
        public ICollection<Company> Company { get; set; }
        
    }
}
