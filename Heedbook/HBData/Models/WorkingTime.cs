using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace HBData.Models
{
    /// <summary>
    ///     Information about working hours of company during week (null - not workin)
    /// </summary>  
    public class WorkingTime
    {
        //   has complex primary key {day, companyId}
        /// <summary>
        ///     Day name
        /// </summary>
        [Required]
        public int Day { get; set; }//Monday - 1, Tuesday - 2..

        /// <summary>
        ///     Id of company
        /// </summary>
        public Guid CompanyId { get; set; }
        [JsonIgnore] public Company Company { get; set; }

        /// <summary>
        ///    Working day start time 
        /// </summary>
        public DateTime? BegTime { get; set; }

        /// <summary>
        ///    Working day end time 
        /// </summary>
        public DateTime? EndTime { get; set; }
    }
}