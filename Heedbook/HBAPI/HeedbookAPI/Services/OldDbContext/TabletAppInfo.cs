using System;
using System.ComponentModel.DataAnnotations;
namespace Old.Models
{
    public class TabletAppInfo
    {
        [Key] public string TabletAppVersion { get; set; }
        public DateTime ReleaseDate { get; set; }
    }
}