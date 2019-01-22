using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostgreSQL.Models
{
    /// <summary>
    /// Application user information
    /// </summary>
    public class ApplicationUser 
    {
        // User id
		public Guid ApplicationUserId { get; set; }
		
        // User full name
        public string FullName { get; set; }

        // Avatar filename
        public string Avatar { get; set; }

        // User email
        public string Email { get; set; }

        // User id in company
        public string EmpoyeeId { get; set; }

        // Creation date of user profile
        public DateTime CreationDate { get; set; }

        // User company
        public int? CompanyId { get; set; }
        public virtual Company Company { get; set; }

        // User status
        public int? StatusId { get; set; }
        public virtual Status Status { get; set; }

        // User id in OneSignal 
        public string OneSignalId { get; set; }

        // User position id
        public int? WorkerTypeId { get; set; }
        public  WorkerType WorkerType { get; set; }

        // Dialogues link
        public virtual ICollection<Dialogue> Dialogue { get; set; }

        // Session list
        public virtual ICollection<Session> Session { get; set; }
    }
}
