using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace HBData.Models
{

    /// <summary>
    /// Adretising (HTML) Content class
    /// </summary>
    public class Content
    {
        [Key]
        public Guid ContentId { get; set; }

        /// <summary>
        /// html code of the content slide
        /// </summary>
        [Required]
        public string RawHTML { get; set; }
        /// <summary>
        /// Name for content
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Duration in milliseconds
        /// </summary>
        public int Duration { get; set; }
        /// <summary>
        /// Company of content
        /// </summary>
        public Guid CompanyId { get; set; }
        [JsonIgnore]
        public Company Company { get; set; }
        /// <summary>
        /// Serialization of editor state
        /// </summary>
        public string JSONData { get; set; }
        /// <summary>
        /// Content is template
        /// </summary>
        public bool IsTemplate { get; set; }
        /// <summary>
        /// Creation date
        /// </summary>
        public DateTime? CreationDate { get; set; }
        /// <summary>
        /// Update date (if it is request on Af  with PUT method -  Update date will be changed on DateTime.Now)
        /// </summary>
        public DateTime? UpdateDate { get; set; }
    }
}
