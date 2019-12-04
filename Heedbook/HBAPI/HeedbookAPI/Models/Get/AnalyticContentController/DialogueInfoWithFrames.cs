using System;
using System.Collections.Generic;
using HBData.Models;

namespace UserOperations.Models.Get.AnalyticContentController
{
    public class DialogueInfoWithFrames
    {
        public Guid DialogueId;
        public Guid ApplicationUserId;
        public DateTime BegTime;
        public DateTime EndTime;
        public DateTime SessionBegTime;
        public DateTime SessionEndTime;
        public List<DialogueFrame> DialogueFrame;
        public double? Age;
        public string Gender;
    }
}