using System;

namespace QuartzExtensions.Model
{
    internal class PhraseResult
    {
        public String Word { get; set; }

        public DateTime BegTime { get; set; }

        public DateTime EndTime { get; set; }

        public Guid? PhraseId { get; set; }

        public Guid? PhraseTypeId { get; set; }

        public Int32 Position { get; set; }     
    }
}