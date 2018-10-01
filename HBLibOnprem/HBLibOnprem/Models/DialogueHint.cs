using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HBLib.Models
{
    public class DialogueHint
    {
        public int DialogueHintId { get; set; }

        //dialogue 
        public Guid? DialogueId { get; set; }
        public virtual Dialogue Dialogue { get; set; }
	
		//hint text
		public string HintText { get; set; }
    }
}
