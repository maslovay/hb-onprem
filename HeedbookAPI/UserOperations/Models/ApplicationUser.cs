using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;


namespace UserOperations.Models
{
   /// <summary>
   /// The Application user class 
   /// Contains parameters of all application users
   /// </summary>
    
    public class ApplicationUser : IdentityUser<Guid>
    {
        //полное имя сотрудника
        public string FullName { get; set; }

        //ссылка на аватар сотрудника
        public string Avatar { get; set; }

        //id сотрудника в компании
        public string EmpoyeeId { get; set; }

        //дата создания
        public DateTime CreationDate { get; set; }

        //компания пользователя
        public Guid? CompanyId { get; set; }
        public Company Company { get; set; }

        //статус пользователя
        public int? StatusId { get; set; }
        public Status Status { get; set; }

        //id пользователей в OneSignal
        public string OneSignalId { get; set; }

        //id position
        
        public Guid? WorkerTypeId { get; set; }
        [ForeignKey("WorkerTypeId")]
        public WorkerType WorkerType { get; set; }

        //links

        //роли сотрудника
        public  ICollection<ApplicationUserRole> UserRoles { get; set; }

        //диалоги сотрудника
        public  ICollection<Dialogue> Dialogue { get; set; }

        //сессии
        public  ICollection<Session> Session { get; set; }
    }
}
