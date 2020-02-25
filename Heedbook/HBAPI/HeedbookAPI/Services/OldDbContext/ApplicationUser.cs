using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace Old.Models
{
    /// <summary>
    ///     The Application user class
    ///     Contains parameters of all application users
    /// </summary>
    public class ApplicationUser : IdentityUser<Guid>
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public override Guid Id
        {
            get => base.Id;
            set => base.Id = value;
        }

        public String FullName { get; set; }

        //ссылка на аватар сотрудника
        public String Avatar { get; set; }

        //id сотрудника в компании
        public String EmpoyeeId { get; set; }

        //дата создания
        public DateTime CreationDate { get; set; }

        //компания пользователя
        public Guid? CompanyId { get; set; }
        public Company Company { get; set; }

        //статус пользователя
        public Int32? StatusId { get; set; }
        [JsonIgnore]
        public Status Status { get; set; }

        //id пользователей в OneSignal
        public String OneSignalId { get; set; }

        //id position

        public Guid? WorkerTypeId { get; set; }

        [ForeignKey("WorkerTypeId")] public WorkerType WorkerType { get; set; }

        //links

        //роли сотрудника
        public ICollection<ApplicationUserRole> UserRoles { get; set; }
        [JsonIgnore]
        //диалоги сотрудника
        public ICollection<Dialogue> Dialogue { get; set; }

        [JsonIgnore]
        //сессии
        public ICollection<Session> Session { get; set; }
        [JsonIgnore]
        //пароли
        public ICollection<PasswordHistory> PasswordHistorys { get; set; }
    }
}