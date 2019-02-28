using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace UserOperations.Models
{
    /// <summary>
    /// Information about dialogue
    /// </summary>
    public class Dialogue
    {
        /// <summary>
        /// Dialogue id
        /// </summary>
        [Key]
        public Guid DialogueId { get; set; }
        /// <summary>
        /// Dialogue creaation time
        /// </summary>
        public DateTime CreationTime { get; set; }
        /// <summary>
        /// Dialogue start time
        /// </summary>
        public DateTime BegTime { get; set; }
        /// <summary>
        /// Dialogue end time
        /// </summary>
        public DateTime EndTime { get; set; }
        /// <summary>
        /// Dialogue's author
        /// </summary>
        public Guid ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }
        /// <summary>
        /// Dilaogue language
        /// </summary>
        public int? LanguageId { get; set; }
        public  Language Language { get; set; }
        /// <summary>
        /// Dialogue status
        /// </summary>
        public int? StatusId { get; set; }
        public  Status Status { get; set; }		
		/// <summary>
        /// System version
        /// </summary>
        public string SysVersion { get; set; }
        /// <summary>
        /// Сonsider dialogue in statistics or not
        /// </summary>
        public bool InStatistic { get; set; }
        /// <summary>
        /// Comment for dialogue
        /// </summary>
        public string Comment { get; set; }
        /// <summary>
        /// Link to client satisfaction
        /// </summary>
        public ICollection<DialogueClientSatisfaction> DialogueClientSatisfaction { get; set; }
        /// <summary>
        /// Link to dialogue audio information
        /// </summary>
        public ICollection<DialogueAudio> DialogueAudio { get; set; }
        /// <summary>
        /// Link to dialogue profile
        /// </summary>
        public ICollection<DialogueClientProfile> DialogueClientProfile { get; set; }
        /// <summary>
        /// Link to information about emotions on frame
        /// </summary>
        public ICollection<DialogueFrame> DialogueFrame { get; set; }
        /// <summary>
        /// Link to some dialogues emotions statistics
        /// </summary>
        public ICollection<DialogueInterval> DialogueInterval { get; set; }
        /// <summary>
        /// Link to phrase count statistics
        /// </summary>
        public  ICollection<DialoguePhraseCount> DialoguePhraseCount { get; set; }
        /// <summary>
        /// Link to speech statistics
        /// </summary>
        public  ICollection<DialogueSpeech> DialogueSpeech { get; set; }
        public  ICollection<DialogueVisual> DialogueVisual { get; set; }
        /// <summary>
        /// Link to words
        /// </summary>
        public ICollection<DialogueWord> DialogueWord { get; set; }

    }
}
