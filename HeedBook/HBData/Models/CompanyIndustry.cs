using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HBData.Models
{
    /// <summary>
    /// Information about indexies of industries such as Load index, Satisfaction index and Cross sales index
    /// </summary>
    public class CompanyIndustry
    {
        /// <summary>
        /// Company iindustry id
        /// </summary>
        [Key]
        public Guid CompanyIndustryId { get; set; }
        /// <summary>
        /// Industry name
        /// </summary>
        public string CompanyIndustryName { get; set; }
        /// <summary>
        /// Value of satisfaction index for companies in industry
        /// </summary>
        public double? SatisfactionIndex { get; set; }
        /// <summary>
        /// Value of load index for companies in industry
        /// </summary>
        public double? LoadIndex { get; set; }
        /// <summary>
        /// Value for cross sales index in industry
        /// </summary>
        public double? CrossSalesIndex { get; set; }
        /// <summary>
        /// Link to companys
        /// </summary>
        public ICollection<Company> Company { get; set; }
        
    }
}
