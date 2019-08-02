using System;
using System.ComponentModel.DataAnnotations;

namespace HbApiTester.Tasks
{
    public class TestTaskWithDelayedResult : TestTask
    {
        public DateTime StartedAt { get; set; }
        public int DelayInMinutes { get; set; }
    }
}