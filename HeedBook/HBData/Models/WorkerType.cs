using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HBData.Models
{
    public class WorkerType
    {
        [Key]
        public int WorkerTypeId { get; set; }

        //company of position
        public Guid CompanyId { get; set; }
        public  Company Company { get; set; }

        //position name
        public string WorkerTypeName { get; set; }
    }
}
