using System;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    /// <summary>
    ///     Information about frame attribute
    /// </summary>
    public class FrameAttribute
    {
        /// <summary>
        ///     Id
        /// </summary>
        [Key]
        public Guid FrameAttributeId { get; set; }

        /// <summary>
        ///     File id
        /// </summary>
        public Guid FileFrameId { get; set; }

        public FileFrame FileFrame { get; set; }

        /// <summary>
        ///     Gender (male or female)
        /// </summary>
        public String Gender { get; set; }

        /// <summary>
        ///     Age
        /// </summary>
        public Double Age { get; set; }

        /// <summary>
        ///     Important values such as face sizes
        /// </summary>
        public String Value { get; set; }

        /// <summary>
        ///     Face descriptor
        /// </summary>
        public String Descriptor { get; set; }
    }
}