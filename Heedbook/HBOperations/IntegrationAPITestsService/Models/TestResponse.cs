using System;
using SQLite;

namespace IntegrationAPITestsService.Models
{
    public class TestResponse
    {
        [PrimaryKey] 
        public Guid ResponseId { get; set; }

        public Guid TaskId { get; set; }
        public string TaskName { get; set; }

        public bool IsFilled { get; set; } = false;
        public bool IsPositive { get; set; }
        public string Info { get; set; }
        public string Body { get; set; }

        public string Url { get; set; }
        
        public string ResultMessage { get; set; }
        
        public DateTime Timestamp { get; set; }
    }
}