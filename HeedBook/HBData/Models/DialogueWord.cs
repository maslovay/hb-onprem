using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HBData.Models
{
    public class DialogueWord
    {
        [Key]
        public Guid DialogueWordId { get; set; }
		 //dialogue 
        public Guid? DialogueId { get; set; }
        public  Dialogue Dialogue { get; set; }
		//client word
		public bool IsClient { get; set; }	
        //слово
        public string Words { get; set; }
    }
}
