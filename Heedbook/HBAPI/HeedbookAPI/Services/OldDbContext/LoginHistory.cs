using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace Old.Models
{
    /// <summary>
    ///     The Error login history class
    ///     Contains parameters of error passwords wich user have written
    /// </summary>
    public class LoginHistory
    {
        [Key]
        public Guid LoginHistoryId { get; set; }
        //дата попытки авторизации
        public DateTime LoginTime { get; set; }
        //Id пользователя
        public Guid UserId { get; set; }
        [JsonIgnore]
        public ApplicationUser User { get; set; }
        //дополнительная информация
        public string Message { get; set; }
        public int Attempt { get; set; }
    }
}