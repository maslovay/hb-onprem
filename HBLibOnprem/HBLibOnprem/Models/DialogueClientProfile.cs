using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HBLib.Models
{
    public class DialogueClientProfile
    {
        public Guid DialogueClientProfileId { get; set; }
		
		//dialogue
		public Guid? DialogueId { get; set; }
        public virtual Dialogue Dialogue { get; set; }
		
		//client or employee
		public bool IsClient { get; set; }
		
        //клиент
        public string Avatar { get; set; }

        //возраст
        public double? Age { get; set; }

        //пол
        public string Gender { get; set; }

    }
}
