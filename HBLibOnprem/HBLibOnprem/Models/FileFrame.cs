using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostgreSQL.Models
{
    /// <summary>
    /// Information about campaign
    /// </summary>
    public class FileFrame
    {
        public Guid FileFrameId { get; set; }

        public Guid ApplicationUserId { get; set; }
        public virtual ApplicationUser ApplicationUser { get; set; }

        public bool FileExist { get; set; }
        public string FileName { get; set; }
        public string FileContainer { get; set; }

        public int StatusId { get; set; }
        public virtual Status Status { get; set; }

        public int StatusNNId { get; set; }
        public virtual Status StatusNN { get; set; }

        public DateTime Time { get; set; }

        public bool IsFacePresent { get; set; }
        public int FaceLength { get; set; }

    }
}
