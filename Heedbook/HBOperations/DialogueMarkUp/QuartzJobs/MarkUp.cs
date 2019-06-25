using System;

namespace DialogueMarkUp.QuartzJobs
{
    public class MarkUp
    {
        public Guid? ApplicationUserId;
        public Guid? FaceId;
        public DateTime BegTime;
        public DateTime EndTime;
        public string BegFileName;
        public string EndFileName;
        public string Descriptor;
    }
}