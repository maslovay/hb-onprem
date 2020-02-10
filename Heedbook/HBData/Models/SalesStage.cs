using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    /// <summary>
    ///     Information about stages of sales (7 stages in DB)
    /// </summary>
    public class SalesStage
    {
        /// <summary>
        ///     Id
        /// </summary>
        [Key]
        public Guid SalesStageId { get; set; }

        /// <summary>
        ///     sales stage number (from 1 to 7)
        /// </summary>
        public int SequenceNumber { get; set; }

        /// <summary>
        ///     text - sales stage name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Links
        /// </summary>
        [JsonIgnore] public ICollection<SalesStagePhrase> SalesStagePhrases { get; set; }
    }
}