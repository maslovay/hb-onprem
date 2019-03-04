using System;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    /// <summary>
    /// Information about dialogue audio file 
    /// </summary>
    public class FileAudioDialogue
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        public Guid FileAudioDialogueId { get; set; }
        /// <summary>
        /// Dialogue id
        /// </summary>
        public Guid DialogueId { get; set; }
        public Dialogue Dialogue { get; set; }
        /// <summary>
        /// Audio beg time
        /// </summary>
        public DateTime BegTime { get; set; }
        /// <summary>
        /// Audio end time
        /// </summary>
        public DateTime EndTime { get; set; }
        /// <summary>
        /// Audio creation time
        /// </summary>
        public DateTime CreationTime { get; set; }
        /// <summary>
        /// Id of google speech recognition request
        /// </summary>
        public string TransactionId {get;set;}
        /// <summary>
        /// Audio filename
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// Audio folder
        /// </summary>
        public string FileContainer { get; set; }
        /// <summary>
        /// Is file exist
        /// </summary>
        public bool FileExist { get; set; }
        /// <summary>
        /// Status id of google recognition
        /// </summary>
        public int? StatusId { get; set; }
        public Status Status { get; set; }
        /// <summary>
        /// Audio duration
        /// </summary>
        public double? Duration { get; set; }
    }
}
