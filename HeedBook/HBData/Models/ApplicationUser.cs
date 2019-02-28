using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HBData.Models
{
   /// <summary>
   /// The Application user class 
   /// Contains parameters of all application users
   /// </summary>
    public class ApplicationUser 
    {
        /// <summary>
        /// User system id
        /// </summary>
        [Key]
		public Guid Id { get; set; }
		
        /// <summary>
        /// User full name
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Filename of avatar on FTP server
        /// </summary>
        public string Avatar { get; set; }

        /// <summary>
        /// User email
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// User id assigned by company
        /// </summary>
        public string EmpoyeeId { get; set; }

        /// <summary>
        /// Creation date of user profile
        /// </summary>
        public DateTime CreationDate { get; set; }

        /// <summary>
        /// User company id
        /// </summary>
        public Guid? CompanyId { get; set; }
        public Company Company { get; set; }

        /// <summary>
        /// User status
        /// </summary>
        public int? StatusId { get; set; }
        public Status Status { get; set; }

        /// <summary>
        /// User id in OneSignal
        /// </summary>        
        public string OneSignalId { get; set; }

        /// <summary>
        /// User worker type id (position)
        /// </summary>
        public int? WorkerTypeId { get; set; }
        public WorkerType WorkerType { get; set; }

        /// <summary>
        /// Dialogue link
        /// </summary>
        public ICollection<Dialogue> Dialogue { get; set; }

        /// <summary>
        /// Session link
        /// </summary>
        public ICollection<Session> Session { get; set; }
    }
}
