using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HbApiTester.Settings;
using Renci.SshNet;

namespace HbApiTester.Tasks
{
    public class TaskFactory
    {
        private readonly HbApiTesterSettings _settings;

        public TaskFactory(HbApiTesterSettings settings) =>
            _settings = settings;
        
        public TestTask GenerateLoginTask()
        {
            var loginTask = new TestTask()
            {
                TaskId = Guid.NewGuid(),
                Url = _settings.ApiAddress + "api/Account/GenerateToken",
                Name = "Login",
                Method = "POST",
                Parameters = new Dictionary<string, string>(),
                Token = String.Empty,
                Body = "{ \"userName\": \""+_settings.User+"\", \"password\": \""+_settings.Password+"\", \"remember\": true}",
                FailMessage = "Can't login!",
                SuccessMessage = "Login successful!"
            };
            return loginTask;
        }    
        
        public TestTask GenerateApiAllCompanyUsersTask(string token)
        {
            var task = new TestTask()
            {
                TaskId = Guid.NewGuid(),
                Url = _settings.ApiAddress + "api/User/User",
                Name = "AllCompanyUsers",
                Method = "GET",
                Parameters = new Dictionary<string, string>(),
                Token = token,
                Body = String.Empty,
                FailMessage = "Can't get users!",
                SuccessMessage = "Get users: success!"
            };
            return task;
        }
        
        public TestTask GenerateApiAssembledDialoguesPresenceTask(string token)
        {
            int timeInHours = 12;
            var task = new TestTask()
            {
                TaskId = Guid.NewGuid(),
                Url = _settings.ApiAddress + "user/Test/CheckIfAnyAssembledDialogues/"+timeInHours,
                Name = "AssembledDialoguesPresence",
                Method = "GET",
                Parameters = new Dictionary<string, string>(),
                Token = token,
                Body = String.Empty,
             };

            task.SuccessMessage = "Dialogs present!";
            task.FailMessage = $"Can't get dialogs for last {timeInHours} hours!";            
            
            return task;
        }


        private TestTask MakeTimeLimitedTask(string token, string taskName, string controller, string function, int days)
        {
            var task = new TestTask()
            {
                TaskId = Guid.NewGuid(),
                Url = _settings.ApiAddress + $"api/{controller}/{function}",
                Name = taskName,
                Method = "GET",
                Parameters = new Dictionary<string, string>(),
                Token = token,
                Body = String.Empty,
            };

            task.Parameters["begTime"] = DateTime.Now.AddDays(-days).ToString("yyyyMMdd");
            task.Parameters["endTime"] = DateTime.Now.ToString("yyyyMMdd");
            task.SuccessMessage = taskName+" is OK!";
            task.FailMessage = $"Can't get {taskName} for last {days} day(s)!";

            return task;
        }

        private TestTask MakeCheckHttpAvailabilityTask(string taskName, string resourceName)
        {
            if ( !_settings.ExternalResources.ContainsKey(resourceName) )
                throw new Exception($"TaskFactory.MakeCheckHttpAvailabilityTask(): can't find a config for {resourceName}");

            var resourceHttpAddress = _settings.ExternalResources[resourceName];
            
            var task = new TestTask()
            {
                TaskId = Guid.NewGuid(),
                Url = resourceHttpAddress,
                Name = taskName,
                Method = "GET",
                Parameters = new Dictionary<string, string>(),
                Token = String.Empty,
                Body = String.Empty,
            };

            task.SuccessMessage = $"{taskName} is OK for resource {resourceName} : {resourceHttpAddress}!";
            task.FailMessage = $"{taskName} is FAILED for resource {resourceName} : {resourceHttpAddress}!";
            
            return task;
        }

        private TestTask MakeCheckFtpAvailabilityTask(string taskName, string resourceName)
        {
            if ( !_settings.ExternalResources.ContainsKey(resourceName) )
                throw new Exception($"TaskFactory.MakeCheckHttpAvailabilityTask(): can't find a config for {resourceName}");

            var resourceAddress = _settings.ExternalResources[resourceName];
            
            var task = new TestTask()
            {
                TaskId = Guid.NewGuid(),
                Url = resourceAddress,
                Name = taskName,
                Method = "ftp",
                Parameters = new Dictionary<string, string>(),
                Token = String.Empty,
                Body = String.Empty,
            };

            task.SuccessMessage = $"{taskName} is OK for SFTP resource!";
            task.FailMessage = $"{taskName} is FAILED for SFTP resource!";

            return task;
        }
        
        public TestTask GenerateApiCheckAnalyticOfficeEfficiencyTask(string token)
            => MakeTimeLimitedTask(token, "CheckAnalyticOfficeEfficiency", "AnalyticOffice", "Efficiency", 1);
        
        public TestTask GenerateApiCheckAnalyticContentEfficiencyTask(string token)
            => MakeTimeLimitedTask(token, "CheckAnalyticContentEfficiency", "AnalyticContent", "Efficiency", 1);

        public TestTask GenerateApiCheckAnalyticContentPollTask(string token)
            => MakeTimeLimitedTask(token, "CheckAnalyticContentPoll", "AnalyticContent", "Poll", 1);
        
        public TestTask GenerateApiCheckAnalyticClientProfileGenderAgeStructureTask(string token)
            => MakeTimeLimitedTask(token, "CheckCheckAnalyticClientProfileGenderAgeStructure", "AnalyticClientProfile", "GenderAgeStructure", 1);

        public TestTask GenerateApiCheckAnalyticRatingProgressTask(string token)
            => MakeTimeLimitedTask(token, "CheckAnalyticRatingProgress", "AnalyticRating", "Progress", 1);
     

        public TestTask GenerateApiCheckAnalyticRatingOfficesTask(string token) 
            => MakeTimeLimitedTask(token, "CheckAnalyticRatingOffices", "AnalyticRating", "RatingOffices", 1);

        public TestTask GenerateApiCheckAnalyticRatingUsersTask(string token)
            => MakeTimeLimitedTask(token, "CheckCheckAnalyticRatingUsers", "AnalyticRating", "RatingUsers", 1);

        
        public TestTask GenerateApiCheckAnalyticReportUserPartialTask(string token)
            => MakeTimeLimitedTask(token, "CheckAnalyticReportUserPartial", "AnalyticReport", "UserPartial", 1);

        public TestTask GenerateApiCheckAnalyticReportUserFullTask(string token)
            => MakeTimeLimitedTask(token, "CheckAnalyticReportUserFull", "AnalyticReport", "UserFull", 1);
        
        public TestTask GenerateApiCheckAnalyticServiceQualityComponentsTask(string token) 
            => MakeTimeLimitedTask(token, "CheckAnalyticServiceQualityComponents", "AnalyticServiceQuality", "Components", 1);

        public TestTask GenerateApiCheckAnalyticServiceQualityDashboardTask(string token)
            => MakeTimeLimitedTask(token, "CheckAnalyticServiceQualityDashboard", "AnalyticServiceQuality", "Dashboard", 1);

        public TestTask GenerateApiCheckAnalyticServiceQualityRatingTask(string token)
            => MakeTimeLimitedTask(token, "CheckAnalyticServiceQualityRating", "AnalyticServiceQuality", "Rating", 1);
        
        public TestTask GenerateApiCheckAnalyticServiceQualitySatisfactionStatsTask(string token)
            => MakeTimeLimitedTask(token, "CheckAnalyticServiceQualitySatisfactionStats", "AnalyticServiceQuality", "SatisfactionStats", 1);
        
        public TestTask GenerateApiCheckAnalyticHomeDashboardTask(string token)
            => MakeTimeLimitedTask(token, "CheckAnalyticHomeDashboard", "AnalyticHome", "Dashboard", 1);
        
        public TestTask GenerateApiCheckAnalyticHomeRecomendationTask(string token)
            => MakeTimeLimitedTask(token, "CheckAnalyticHomeRecomendation", "AnalyticHome", "Recomendation", 1);
        
        public TestTask GenerateApiCheckAnalyticSpeechEmployeeRatingTask(string token)
            => MakeTimeLimitedTask(token, "CheckAnalyticSpeechEmployeeRating", "AnalyticSpeech", "EmployeeRating", 1);
        
        public TestTask GenerateApiCheckAnalyticSpeechPhraseTableTask(string token)
            => MakeTimeLimitedTask(token, "CheckAnalyticSpeechPhraseTable", "AnalyticSpeech", "PhraseTable", 1);
        
        public TestTask GenerateApiCheckAnalyticSpeechPhraseTypeCountTask(string token)
            => MakeTimeLimitedTask(token, "CheckAnalyticSpeechPhraseTypeCount", "AnalyticSpeech", "PhraseTypeCount", 1);
        
        public TestTask GenerateApiCheckAnalyticSpeechWordCloudTask(string token)
            => MakeTimeLimitedTask(token, "CheckAnalyticSpeechPhraseTypeCount", "AnalyticSpeech", "WordCloud", 1);

        public TestTask GenerateExternalResourceCheckKibanaAvailabilityTask()
            => MakeCheckHttpAvailabilityTask("CheckKibanaAvailability", "Kibana");
        public TestTask GenerateExternalResourceCheckFtpAvailabilityTask()
            => MakeCheckFtpAvailabilityTask("CheckFtpAvailability", "Ftp");
    }
}