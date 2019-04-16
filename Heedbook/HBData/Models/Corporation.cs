using System;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    /// <summary>
    ///     List of corporations
    /// </summary>
    public class Corporation
    {
        /// <summary>
        ///     Corporation id
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        ///     Corporation name
        /// </summary>
        public String Name { get; set; }
    }
}