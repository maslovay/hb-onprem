using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;


namespace HBData.Models
{
    /// <summary>
    /// Information about frame attribute
    /// </summary>
    public class FrameAttribute
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        public Guid FrameAttributeId { get; set; }
        /// <summary>
        /// File id
        /// </summary>
        public Guid FileFrameId { get; set; }
        public FileFrame FileFrame { get; set; }
        /// <summary>
        /// Gender (male or female)
        /// </summary>
        public string Gender { get; set; }
        /// <summary>
        /// Age 
        /// </summary>
        public double Age { get; set; }
        /// <summary>
        /// Important values such as face sizes 
        /// </summary>
        public string Value {get;set;}
        /// <summary>
        /// Face descriptor 
        /// </summary>
        public string Descriptor {get;set;}
    }
}
