using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        [JsonIgnore] public ICollection<Company> Companies { get; set; }
    }
}