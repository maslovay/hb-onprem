using System.Collections.Generic;

namespace HBData.Models
{
    public class Country
    {
        public int CountryId { get; set; }

        //название страны
        public string CountryName { get; set; }

        //links
        //компании языка
        public  ICollection<Company> Company { get; set; }
        
    }
}
