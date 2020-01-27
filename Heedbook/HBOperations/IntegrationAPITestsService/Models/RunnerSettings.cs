using System.Collections.Generic;

namespace IntegrationAPITestsService.Models
{
    public class RunnerSettings
    {
        public string User { get; set; }
        public string Password { get; set; }
        public int CheckPeriodMin { get; set; }
        public string ApiAddress { get; set; }
        public ICollection<string> Tests  { get; set; }
        public ICollection<string> Handlers { get; set; }
        public Dictionary<string, string> ExternalResources { get; set; }
    }
}