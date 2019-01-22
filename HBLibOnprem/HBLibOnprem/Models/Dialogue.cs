using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PostgreSQL.Models
{
    public class Dialogue
    {
        public Guid DialogueId { get; set; }

        //creation date
        public DateTime CreationTime { get; set; }

        //время начала диалога
        public DateTime BegTime { get; set; }

        //время окончания
        public DateTime EndTime { get; set; }
			
        //автор диалога (сотрудник)
        public Guid ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        //язык 
        public int? LanguageId { get; set; }
        public  Language Language { get; set; }

        //статус
        public int? StatusId { get; set; }
        public  Status Status { get; set; }
					
		//версия системы
        public string SysVersion { get; set; }

        //учитывается в статистике
        public bool InStatistic { get; set; }		
		
		//comment
		public string Comment { get; set; }


        //links
        public  ICollection<DialogueClientSatisfaction> DialogueClientSatisfaction { get; set; }

        public  ICollection<DialogueAudio> DialogueAudio { get; set; }       

        public  ICollection<DialogueClientProfile> DialogueClientProfile { get; set; }

        public  ICollection<DialogueFrame> DialogueFrame { get; set; }

        public  ICollection<DialogueInterval> DialogueInterval { get; set; }

        public  ICollection<DialoguePhraseCount> DialoguePhraseCount { get; set; }

        public  ICollection<DialogueSpeech> DialogueSpeech { get; set; }

        public  ICollection<DialogueVisual> DialogueVisual { get; set; }

        public  ICollection<DialogueWord> DialogueWord { get; set; }

    }
}
