namespace HBData.Models
{
    public class PhraseCompany
    {
        public int PhraseCompanyId { get; set; }

        //phrase 
        public int? PhraseId { get; set; }
        public  Phrase Phrase { get; set; }

        //Company 
        public int? CompanyId { get; set; }
        public  Company Company { get; set; }

    }
}
