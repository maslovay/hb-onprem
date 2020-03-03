using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HBData.Models
{
    public class DeviceType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid DeviceTypeId { get; set; }

        //device type name
        public String Name { get; set; }
    }
}