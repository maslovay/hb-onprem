using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HBData.Models
{
    /// <summary>
    /// Information about worker position in company
    /// </summary>
    public class WorkerType
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid WorkerTypeId { get; set; }
        /// <summary>
        /// Company id
        /// </summary>
        public Guid CompanyId { get; set; }
        public  Company Company { get; set; }
        /// <summary>
        /// Position name
        /// </summary>
        public string WorkerTypeName { get; set; }
    }
}
