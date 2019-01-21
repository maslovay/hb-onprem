using System;
using HBLib.Models;

namespace HBData.Models
{
    public class DialogueWord
    {
        public Guid DialogueWordId { get; set; }
				
		 //dialogue 
        public Guid? DialogueId { get; set; }
        public  Dialogue Dialogue { get; set; }
		
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
