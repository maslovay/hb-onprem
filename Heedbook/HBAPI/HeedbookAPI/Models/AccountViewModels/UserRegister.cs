using System;
using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace UserOperations.AccountModels
{
    [SwaggerTag("Data for creating new user and company")]
    public class UserRegister
    {       
        public string FullName;
        [Required]
        public string Email;
        public string Password;
        [Required]
        public string CompanyName;
        [Required]
        public int LanguageId;
        [Required]
        public Guid CountryId;
        [Required]
        public Guid CompanyIndustryId; 
        public Guid? CorporationId;
        public string Role;
        public bool? IsExtended;
        [Required]
        public string TimeZone;
        //---working hours
        public DateTime? MondayBeg;
        public DateTime? MondayEnd;

        public DateTime? TuesdayBeg;
        public DateTime? TuesdayEnd;

        public DateTime? WednesdayBeg;
        public DateTime? WednesdayEnd;

        public DateTime? ThursdayBeg;
        public DateTime? ThursdayEnd;

        public DateTime? FridayBeg;
        public DateTime? FridayEnd;

        public DateTime? SaturdayBeg;
        public DateTime? SaturdayEnd;

        public DateTime? SundayBeg;
        public DateTime? SundayEnd;
    }
}