using System;
using System.ComponentModel.DataAnnotations;

namespace IntegrationAPITestsService.Models
{
    public class TestTaskWithDelayedResult : TestTask
    {
        public DateTime StartedAt { get; set; }
        public int DelayInMinutes { get; set; }
    }
}