using System;
using HBLib.Models;

namespace HBData.Models
{
    public class Session
    {
        public int SessionId { get; set; }

        //сотрудник
        public string ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        //начало сессии
        public DateTime BegTime { get; set; }

        //окончание сессии
        public DateTime EndTime { get; set; }

        //статус сессии
        public int? StatusId { get; set; }
        public Status Status { get; set; }

        //desktop session
        public bool IsDesktop { get; set; }
    }
}