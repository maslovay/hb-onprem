using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace Old.Models
{
    /// <summary>
    ///     The Password history class
    ///     Contains parameters of passwords wich user have used (for saving 5 last passwords)
    /// </summary>
    public class PasswordHistory
    {
        [Key]
        public Guid PasswordHistoryId { get; set; }
        //дата создания пароля
        public DateTime CreationDate { get; set; }
        //Id пользователя
        public Guid UserId { get; set; }
        [JsonIgnore]
        public ApplicationUser User { get; set; }
        //пароль
        public string PasswordHash { get; set; }
    }
}