using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HBData.Models
{
    public class DialogueMarkup
    {
        [Key]
        public Guid DialogueMarkUpId { get; set; }
        public Guid? ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser{ get; set; }
		public DateTime BegTime { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime BegTimeMarkup { get; set; }
        public DateTime EndTimeMarkup { get; set; }
        public bool IsDialogue { get; set; }
        public int StatusId { get; set; }
        public Status Status { get; set; }
        public string TeacherId { get; set; }
    }
}
