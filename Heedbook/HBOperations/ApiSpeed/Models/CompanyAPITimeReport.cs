namespace ApiPerformance.Models
{
    public class CompanyAPITimeReport
    {
        public string CompanyName {get; set;}
        public ResponceReportModel AccountRegister {get; set;}
        public ResponceReportModel AccountGenerateToken {get; set;}
        public ResponceReportModel AccountChangePassword {get; set;}
        public ResponceReportModel AccountUnblock {get; set;}

        public ResponceReportModel AnalyticClientProfileGenderAgeStructure {get; set;}

        public ResponceReportModel AnalyticContentContentShows {get; set;}
        public ResponceReportModel AnalyticContentEfficiency {get; set;}
        public ResponceReportModel AnalyticContentPool {get; set;}

        public ResponceReportModel AnalyticHomeDashboard {get; set;}

        public ResponceReportModel AnalyticOfficeEfficiency {get; set;}

        public ResponceReportModel AnalyticRatingProgress {get; set;}
        public ResponceReportModel AnalyticRatingRatingUsers {get; set;}
        public ResponceReportModel AnalyticRatingOffices {get; set;}

        public ResponceReportModel AnalyticReportActiveEmployee {get; set;}
        public ResponceReportModel AnalyticReportUserPartial {get; set;}
        public ResponceReportModel AnalyticReportUserFull {get; set;}

        public ResponceReportModel AnalyticServiceQualityComponent {get; set;}
        public ResponceReportModel AnalyticServiceQualityDashboard {get; set;}
        public ResponceReportModel AnalyticServiceQualityRating {get; set;}
        public ResponceReportModel AnalyticServiceQualitySatisfactionStats {get; set;}

        public ResponceReportModel AnalyticSpeechEmployeeRating {get; set;}
        public ResponceReportModel AnalyticSpeechEmployeePhraseTable {get; set;}
        public ResponceReportModel AnalyticSpeechPhraseTypeCount {get; set;}
        public ResponceReportModel AnalyticSpeechWordCloud {get; set;}

        public ResponceReportModel AnalyticWeeklyReportUser {get; set;}

        public ResponceReportModel CampaignContentReturnCampaignWithContent {get; set;}
        public ResponceReportModel CampaignContentCreateCampaignWithContent {get; set;}
        public ResponceReportModel CampaignContentGetAllContent {get; set;}
        public ResponceReportModel CampaignContentSaveNewContent {get; set;}

        public ResponceReportModel CatalogueCountry {get; set;}
        public ResponceReportModel CatalogueRole {get; set;}
        public ResponceReportModel CatalogueDeviceType {get; set;}
        public ResponceReportModel CatalogueIndustry {get; set;}
        public ResponceReportModel CatalogueLanguage {get; set;}
        public ResponceReportModel CataloguePhraseType {get; set;}
        public ResponceReportModel CatalogueAlertType {get; set;}

        public ResponceReportModel CompanyReportGetReport {get; set;}

        public ResponceReportModel DemonstrationFlushStats {get; set;}
        public ResponceReportModel DemonstrationGetContents {get; set;}
        public ResponceReportModel DemonstrationPoolAnswer {get; set;}

        public ResponceReportModel HelpGetIndex {get; set;}
        public ResponceReportModel HelpGetDatabaseFilling {get; set;}        

        public ResponceReportModel LoggingSendLogGet {get; set;}
        public ResponceReportModel LoggingSendLogPost {get; set;}

        public ResponceReportModel MediaFileFileGet {get; set;}
        public ResponceReportModel MediaFilePost {get; set;}

        public ResponceReportModel PaymentTariff {get; set;}
        public ResponceReportModel PaymentCheckoutResponce {get; set;}

        public ResponceReportModel PhrasePhraseScript {get; set;}

        public ResponceReportModel SessionSessionStatus {get; set;}
        public ResponceReportModel SessionAlertNotSmile {get; set;}

        public ResponceReportModel SiteFeedBack {get; set;}

        public ResponceReportModel UserGetAllCompanyUsers {get; set;}        
        public ResponceReportModel UserPost {get; set;}
        public ResponceReportModel UserCompanies {get; set;}
        public ResponceReportModel UserCorporations {get; set;}
        public ResponceReportModel UserPhraseLibLibrary {get; set;}        
        public ResponceReportModel UserPhraseLibCreateCompanyPhrase {get; set;}
        public ResponceReportModel UserCompanyPhraseReturnAttachedToCompanyPhrases {get; set;}
        public ResponceReportModel UserCompanyPhraseAttachLibraryPhrasesToCompany {get; set;}
        public ResponceReportModel UserDialogue {get; set;}
        public ResponceReportModel UserDialogueInclude {get; set;}
        public ResponceReportModel UserAlert {get; set;}
    }
}