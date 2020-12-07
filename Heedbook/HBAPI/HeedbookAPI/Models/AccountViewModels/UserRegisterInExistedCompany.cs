using System;
using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace UserOperations.AccountModels
{
    [SwaggerTag("Data for creating new user in existed company")]
    public class UserRegisterInExistedCompany
    {       
        public string FullName;
        [Required]
        public string Email;
        public string Password;
        public Guid CompanyId;
        [Required]
        public string Role;
    }
}