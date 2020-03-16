using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    /// <summary>
    ///     Information about indexies of industries such as Load index, Satisfaction index and Cross sales index
    /// </summary>
    public class CompanyIndustry
    {
        /// <summary>
        ///     Company iindustry id
        /// </summary>
        [Key]
        public Guid CompanyIndustryId { get; set; }

        /// <summary>
        ///     Industry name
        /// </summary>
        public String CompanyIndustryName { get; set; }

        /// <summary>
        ///     Value of satisfaction index for companies in industry
        /// </summary>
        public Double? SatisfactionIndex { get; set; }

        /// <summary>
        ///     Value of load index for companies in industry
        /// </summary>
        public Double? LoadIndex { get; set; }

        /// <summary>
        ///     Value for cross sales index in industry
        /// </summary>
        public Double? CrossSalesIndex { get; set; }

        /// <summary>
        ///     Link to companys
        /// </summary>
        public ICollection<Company> Company { get; set; }
    }
}