using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace UserOperations.Models
{
    /// <summary>
    /// List of corporations
    /// </summary>
    public class Corporation
    {
        /// <summary>
        /// Corporation id
        /// </summary>
        [Key]
        public Guid Id { get; set; }
        /// <summary>
        /// Corporation name
        /// </summary>
        public string Name { get; set; }
    }
}
