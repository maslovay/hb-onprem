using System;

namespace ApiPerformance.Models
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
        public Guid? CorporationId;
        public string Role;
        public bool? IsExtended;
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