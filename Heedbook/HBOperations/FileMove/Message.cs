using System;

namespace FileMove
{
    public class Message
    {
        public Guid ApplicationUserId { get; set; }
        public Guid DialogueId { get; set; }
        public DateTime BeginTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}