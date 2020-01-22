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
        public string TimeZoneName;
        public bool? IsExtended;
    }
}