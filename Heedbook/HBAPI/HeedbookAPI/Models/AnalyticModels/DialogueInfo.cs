using HBData.Models;
using System;
using System.Collections.Generic;

namespace UserOperations.Models.AnalyticModels
{
    public class DialogueInfo
    {
        public Guid? IndustryId;//---!!!for benchmarks only
        public Guid? CompanyId;//---!!!for benchmarks only
        public Guid DialogueId;
        public Guid? ApplicationUserId;
        public Guid DeviceId;
        public DateTime BegTime;
        public DateTime EndTime;
        public string FullName;
        public string DeviceName;
        public int CrossCount;
        public int AlertCount;
        public double? SatisfactionScore;
        public double? SatisfactionScoreBeg;
        public double? SatisfactionScoreEnd;
        public double? SmilesShare;
    }

    public class DialogueInfoCompany
    {
        public Guid DialogueId;
        public Guid CompanyId;
        public DateTime BegTime;
        public DateTime EndTime;
        public string FullName;
        public int CrossCount;
        public double? SatisfactionScore;
        public double? SatisfactionScoreBeg;
        public double? SatisfactionScoreEnd;
        public DateTime SessionBegTime;
        public DateTime SessionEndTime;
    }
    public class DialogueInfoFull
    {
        public Guid? IndustryId;//---!!!for benchmarks only
        public Guid? CompanyId;//---!!!for benchmarks only
        public Guid DialogueId;
        public Guid? ApplicationUserId;
        public Guid DeviceId;
        public DateTime BegTime;
        public DateTime EndTime;
        public string FullName;
        public int CrossCount;
        public int AlertCount;
        public double? SatisfactionScore;
        public double? SatisfactionScoreBeg;
        public double? SatisfactionScoreEnd;
        public DateTime SessionBegTime;
        public DateTime SessionEndTime;
    }

    public class DialogueInfoWithFrames
    {
        public Guid DialogueId;
        public Guid? ApplicationUserId;
        public Guid DeviceId;
        public DateTime BegTime;
        public DateTime EndTime;
        public DateTime SessionBegTime;
        public DateTime SessionEndTime;
        public List<DialogueFrame> DialogueFrame;
        public double? Age;
        public string Gender;
    }

}