using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace HBData.Models
{
    /// <summary>
    /// The Application user class 
    /// Contains parameters of all application users
    /// </summary>
    
    public class ApplicationUser : IdentityUser<Guid>
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public override Guid Id
        {
            get { return base.Id; }
            set { base.Id = value; }
        }
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
