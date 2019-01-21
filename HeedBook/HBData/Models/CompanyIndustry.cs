using System.Collections.Generic;

namespace HBData.Models
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
