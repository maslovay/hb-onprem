using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace HBData.Models
{
    /// <summary>
    ///     Adretising (HTML) Content class
    /// </summary>
    public class Content
    {
        [Key] public Guid ContentId { get; set; }
        //public int? StatusId { get; set; }
        //[JsonIgnore] public Status Status { get; set; }

        /// <summary>
        ///     html code of the content slide
        /// </summary>
        [Required]
        public String RawHTML { get; set; }

        /// <summary>
        ///     Name for content
        /// </summary>
        public String Name { get; set; }

        /// <summary>
        ///     Duration in milliseconds
        /// </summary>
        public Int32 Duration { get; set; }

        /// <summary>
        ///     Company of content
        /// </summary>
        public Guid? CompanyId { get; set; }

        [JsonIgnore] public Company Company { get; set; }

        /// <summary>
        ///     Serialization of editor state
        /// </summary>
        public String JSONData { get; set; }

        /// <summary>
        ///     Content is template
        /// </summary>
        public Boolean IsTemplate { get; set; }

        /// <summary>
        ///     Creation date
        /// </summary>
        public DateTime? CreationDate { get; set; }

        /// <summary>
        ///     Update date (if it is request on Af  with PUT method -  Update date will be changed on DateTime.Now)
        /// </summary>
        public DateTime? UpdateDate { get; set; }

        public Int32? StatusId { get; set; }
        [JsonIgnore] public Status Status { get; set; }

        [JsonIgnore] public ICollection<CampaignContent> CampaignContents { get; set; }
    }
}