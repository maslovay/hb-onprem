using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HBLib.Models;

namespace HBData.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser
    {
        [Key]
        public String Id { get; set; }

        //полное имя сотрудника
        public string FullName { get; set; }

        //ссылка на аватар сотрудника
        public string Avatar { get; set; }

        //почта сотрудника
        public string Email { get; set; }

        //id сотрудника в компании
        public string EmployeeId { get; set; }

        //дата создания
        public DateTime CreationDate { get; set; }

        //компания пользователя
        public Int32? CompanyId { get; set; }
        public Company Company { get; set; }

        /// <summary>
        /// Статус пользователя
        /// </summary>
        public int? StatusId { get; set; }
        public Status Status { get; set; }

        //id пользователей в OneSignal
        public string OneSignalId { get; set; }

        //id position
        public int? WorkerTypeId { get; set; }
        public WorkerType WorkerType { get; set; }

        //links

        /// <summary>
        /// Диалоги сотрудников
        /// </summary>
        public ICollection<Dialogue> Dialogue { get; set; }

        //сессии
        public ICollection<Session> Session { get; set; }
    }
}