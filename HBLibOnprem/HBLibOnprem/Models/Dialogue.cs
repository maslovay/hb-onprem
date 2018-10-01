using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HBLib.Models
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
        public string ApplicationUserId { get; set; }
        public virtual ApplicationUser ApplicationUser { get; set; }

        //язык 
        public int? LanguageId { get; set; }
        public virtual Language Language { get; set; }

        //статус
        public int? StatusId { get; set; }
        public virtual Status Status { get; set; }
					
		//версия системы
        public string SysVersion { get; set; }

        //учитывается в статистике
        public bool InStatistic { get; set; }		
		
		//comment
		public string Comment { get; set; }


        //links
        public virtual ICollection<DialogueClientSatisfaction> DialogueClientSatisfaction { get; set; }

        public virtual ICollection<DialogueAudio> DialogueAudio { get; set; }       

        public virtual ICollection<DialogueClientProfile> DialogueClientProfile { get; set; }

        public virtual ICollection<DialogueFrame> DialogueFrame { get; set; }

        public virtual ICollection<DialogueInterval> DialogueInterval { get; set; }

        public virtual ICollection<DialoguePhraseCount> DialoguePhraseCount { get; set; }

        public virtual ICollection<DialoguePhrasePlace> DialoguePhrasePlace { get; set; }

        public virtual ICollection<DialogueSpeech> DialogueSpeech { get; set; }

        public virtual ICollection<DialogueVisual> DialogueVisual { get; set; }

        public virtual ICollection<DialogueWord> DialogueWord { get; set; }

    }
}
