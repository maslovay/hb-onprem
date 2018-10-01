using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HBLib.Models
{
    public class DialogueWord
    {
        public Guid DialogueWordId { get; set; }
				
		 //dialogue 
        public Guid? DialogueId { get; set; }
        public virtual Dialogue Dialogue { get; set; }
		
		//client word
		public bool IsClient { get; set; }	
	
        //слово
        public string Word { get; set; }

        //время начала слова
        public DateTime BegTime { get; set; }

        //время окончания слова
        public DateTime EndTime { get; set; }

    }
}
