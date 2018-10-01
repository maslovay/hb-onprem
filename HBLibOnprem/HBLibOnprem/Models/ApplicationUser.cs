using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HBLib.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser 
    {
		public string Id { get; set; }
		
        //полное имя сотрудника
        public string FullName { get; set; }

        //ссылка на аватар сотрудника
        public string Avatar { get; set; }

        //почта сотрудника
        public string Email { get; set; }

        //id сотрудника в компании
        public string EmpoyeeId { get; set; }

        //дата создания
        public DateTime CreationDate { get; set; }

        //компания пользователя
        public int? CompanyId { get; set; }
        public virtual Company Company { get; set; }

        //статус пользователя
        public int? StatusId { get; set; }
        public virtual Status Status { get; set; }

        //id пользователей в OneSignal
        public string OneSignalId { get; set; }

        //id position
        public int? WorkerTypeId { get; set; }
        public virtual WorkerType WorkerType { get; set; }

        //links

        //диалоги сотрудника
        public virtual ICollection<Dialogue> Dialogue { get; set; }

        //сессии
        public virtual ICollection<Session> Session { get; set; }
    }
}
