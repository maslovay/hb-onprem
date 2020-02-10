using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace HBData.Models
{
    /// <summary>
    ///     Connection stages of sales to phrase and company (
    /// </summary>
    public class SalesStagePhrase
    {
        /// <summary>
        ///     Id
        /// </summary>
        [Key]
        public Guid SalesStagePhraseId { get; set; }
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
        /// <summary>
        ///     wich phrase has been cnnected to SalesStage
        /// </summary>
        public Guid PhraseId { get; set; }
        public Phrase Phrase { get; set; }
        /// <summary>
        ///     wich SalesStage has been connected with phrase
        /// </summary>
        public Guid SalesStageId { get; set; }
        public SalesStage SalesStage { get; set; }

    }
}