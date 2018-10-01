using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HBLib.Models
{
    public class Company
    {
        public int CompanyId { get; set; }

        //название компании
        [Required]
        public string CompanyName { get; set; }

        //отрасль компании
		public int? CompanyIndustryId { get; set; }
        public virtual CompanyIndustry CompanyIndustry { get; set; }

        //дата создания
        public DateTime CreationDate { get; set; }

        //язык компании
        public int LanguageId { get; set; }
        public virtual Language Language { get; set; }

        //страна компании
        public int? CountryId { get; set; }
        public virtual Country Country { get; set; }

        //status company
        public int? StatusId { get; set; }
        public virtual Status Status { get; set; }

        //links
        //сотрудники компании
        public virtual ICollection<ApplicationUser> ApplicationUser { get; set; }

        //оплаты компании
        public virtual ICollection<Payment> Payment { get; set; }
        
        //корпорация, к которой принадлежит компания. Может быть пустой.
        public int? CorporationId { get; set; }
        public virtual Corporation Corporation { get; set; }
    }
}
