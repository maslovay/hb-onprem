using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HBData.Models
{
    public class Company
    {
        [Key]
        public Guid CompanyId { get; set; }

        //название компании
        [Required]
        public string CompanyName { get; set; }

        //отрасль компании
		public int? CompanyIndustryId { get; set; }
        public  CompanyIndustry CompanyIndustry { get; set; }

        //дата создания
        public DateTime CreationDate { get; set; }

        //язык компании
        public int LanguageId { get; set; }
        public  Language Language { get; set; }

        //страна компании
        public int? CountryId { get; set; }
        public  Country Country { get; set; }

        //status company
        public int? StatusId { get; set; }
        public  Status Status { get; set; }

        //links
        //сотрудники компании
        public  ICollection<ApplicationUser> ApplicationUser { get; set; }

        //оплаты компании
        public  ICollection<Payment> Payment { get; set; }
        
        //корпорация, к которой принадлежит компания. Может быть пустой.
        public int? CorporationId { get; set; }
        public  Corporation Corporation { get; set; }
    }
}
