using System;

namespace UserOperations.AccountModels
{
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