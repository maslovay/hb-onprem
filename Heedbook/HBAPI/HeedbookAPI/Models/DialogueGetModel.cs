using System;

namespace UserOperations.Models
{
    public class DialogueGetModel
    {
        public Guid DialogueId;
        public string Avatar;
        public Guid? ApplicationUserId;
        public string FullName;
        public string DialogueHints;
        public DateTime BegTime;
        public DateTime EndTime;
        public TimeSpan Duration;
        public int? StatusId;
        public bool InStatistic;
        public double? MeetingExpectationsTotal;
        public Guid DeviceId;
        public string DeviceName;
    }
}