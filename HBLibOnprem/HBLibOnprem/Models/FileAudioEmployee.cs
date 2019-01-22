using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostgreSQL.Models
{
    /// <summary>
    /// Information about audios in storage
    /// </summary>
    public class FileAudioEmployee
    {
        public Guid FileAudioEmployeeId { get; set; }

        public Guid ApplicationUserId { get; set; }
        public virtual ApplicationUser ApplicationUser { get; set; }

        public DateTime BegTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime CreationTime {get; set;}

        public string FileName { get; set; }
        public string FileContainer { get; set; }

        public bool FileExist { get; set; }
        public int? StatusId { get; set; }
        public virtual Status Status { get; set; }

        public double? Duration { get; set; }
    }
}
