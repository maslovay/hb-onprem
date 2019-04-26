using System;
using Swashbuckle.AspNetCore.Annotations;

namespace UserOperations.AccountModels
{
    [SwaggerTag("Data for creating new user and company")]
    public class UserRegister
    {
        public string FullName;
        public string Email;
        public string Password;
        public string CompanyName;
        public int LanguageId;
        public Guid CountryId;
        public Guid CompanyIndustryId; 
    }

}