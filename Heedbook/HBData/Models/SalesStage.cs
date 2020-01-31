using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

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
        ///    if the stage is specific to an individual company, then fill out this field
        /// </summary>
        public Guid? CompanyId { get; set; }
        public Company Company { get; set; }

        /// <summary>
        ///    if the stage is specific to an individual corporation, then fill out this field
        /// </summary>
        public Guid? CorporationId { get; set; }
        public Corporation Corporation { get; set; }
        //[JsonIgnore]
        ///// <summary>
        /////     Links
        ///// </summary>
        //public ICollection<Phrase> Phrases { get; set; }
    }
}