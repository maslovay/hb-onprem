using System;
using Swashbuckle.AspNetCore.Annotations;

namespace UserOperations.AccountModels
{
    [SwaggerTag("Data for creating new user and company")]
    public class UserRegister
    {
     //   public Guid Id { get; set; }//delete
        public string FullName;
        public string Email;
        public string Password;
        public string CompanyName;
        public int LanguageId;
        public Guid CountryId;
        public Guid CompanyIndustryId; 
        public Guid? CorporationId;      

    }


     public class UserRegister2
    {
        public string Id { get; set; }//delete
        public string FullName;
        public string Email;
        public string Password;
        public string CompanyName;
        public int LanguageId;
        public string CountryId;
        public string CompanyIndustryId; 
        public string CorporationId; 

        //--------FOR DELETE-------
         public int? StatusId; 
        public string WorkerTypeId; 
           public string CompanyId; 
             public string UserName;
               public string CreationDate;

    }

}