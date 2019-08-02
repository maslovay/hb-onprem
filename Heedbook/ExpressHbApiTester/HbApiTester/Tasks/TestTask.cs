using System;
using System.Collections.Generic;
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace HbApiTester.Tasks
{
    public class TestTask
    {
        [PrimaryKey] 
        public Guid TaskId { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string Method { get; set; }
        [TextBlob("Parameters")]
        public Dictionary<string, string> Parameters { get; set; }
        public string Token{ get; set; }
        
        public string FailMessage { get; set; } = string.Empty;

        public string SuccessMessage { get; set; } = string.Empty;
        
        public string Body { get; set; } = string.Empty;
    }
}