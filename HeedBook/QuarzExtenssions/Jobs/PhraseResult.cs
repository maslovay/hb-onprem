﻿using System;

namespace QuartzExtensions.Jobs
{
    internal class PhraseResult
    {
        public String Word { get; set; }

        public DateTime BegTime { get; set; }

        public DateTime EndTime { get; set; }

        public Guid? PhraseId { get; set; }

        public Guid? PhraseTypeId { get; set; }
    }
}