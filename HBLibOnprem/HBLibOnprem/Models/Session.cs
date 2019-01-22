using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PostgreSQL.Models
{
    public class Session
    {
        public int SessionId { get; set; }

        //сотрудник
        public Guid ApplicationUserId { get; set; }
        [JsonIgnore]
        public  ApplicationUser ApplicationUser { get; set; }

        //начало сессии
        public DateTime BegTime { get; set; }

        //окончание сессии
        public DateTime EndTime { get; set; }

        //статус сессии
        public int? StatusId { get; set; }
        public  Status Status { get; set; }


        //desktop session
        public bool IsDesktop { get; set; }


    }
}
