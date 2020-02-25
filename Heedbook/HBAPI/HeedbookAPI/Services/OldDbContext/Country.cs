using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Old.Models
{
    /// <summary>
    ///     Information abount countries
    /// </summary>
    public class Country
    {
        /// <summary>
        ///     Country id
        /// </summary>
        [Key]
        public Guid CountryId { get; set; }

        /// <summary>
        ///     Country name
        /// </summary>
        public String CountryName { get; set; }

        /// <summary>
        ///     Link to companys with this language
        /// </summary>
        [JsonIgnore] public ICollection<Company> Company { get; set; }
    }
}