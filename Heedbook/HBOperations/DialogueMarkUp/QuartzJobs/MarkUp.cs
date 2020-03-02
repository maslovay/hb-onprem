using System;
using System.Collections.Generic;
using HBData.Models;

namespace DialogueMarkUp.QuartzJobs
{
    public class MarkUp
    {
        public Guid? ApplicationUserId;
        public Guid DeviceId;
        public Guid? FaceId;
        public DateTime BegTime;
        public DateTime EndTime;
        public List<FileFrame> FileNames;
        public string Descriptor;
        public string Gender;
        public List<FileVideo> Videos;
    }
}