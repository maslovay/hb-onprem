using System;
using System.ComponentModel.DataAnnotations;
namespace HBData.Models
{
    public class TabletAppInfo
    {
        [Key] public string TabletAppVersion { get; set; }
        public DateTime ReleaseDate { get; set; }
    }
}