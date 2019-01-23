using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;


namespace HBData.Models
{
    public class FileAudioDialogue
    {
        [Key]
        public Guid FileAudioDialogueId { get; set; }
        public Guid DialogueId { get; set; }
        public Dialogue Dialogue { get; set; }

        public DateTime BegTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime CreationTime { get; set; }
        public string FileName { get; set; }
        public string FileContainer { get; set; }
        public bool FileExist { get; set; }
        public int StatusId { get; set; }
        public Status Status { get; set; }
        public double? Duration { get; set; }
    }
}
