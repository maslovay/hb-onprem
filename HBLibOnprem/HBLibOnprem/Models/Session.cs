using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HBLib.Models
{
    public class Session
    {
        public int SessionId { get; set; }

        //сотрудник
        public string ApplicationUserId { get; set; }
        public virtual ApplicationUser ApplicationUser { get; set; }

        //начало сессии
        public DateTime BegTime { get; set; }

        //окончание сессии
        public DateTime EndTime { get; set; }

        //статус сессии
        public int? StatusId { get; set; }
        public virtual Status Status { get; set; }


        //desktop session
        public bool IsDesktop { get; set; }


    }
}
