using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Old.Models
{
    /// <summary>
    /// record about client
    /// </summary>
    public class ClientNote
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ClientNoteId { get; set; }

        /// <summary>
        /// Id of the client  -  this record about whom
        /// </summary>
        public Guid ClientId { get; set; }
        [JsonIgnore] public Client Client { get; set; }

        /// <summary>
        /// record creation date
        /// </summary>
        public DateTime CreationDate { get; set; }

        /// <summary>
        /// Link to record author
        /// </summary>
        public Guid? ApplicationUserId { get; set; }
        [JsonIgnore] public ApplicationUser ApplicationUser { get; set; }

        /// <summary>
        /// text of this record
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// list of tags in this record
        /// </summary>
        public string[] Tags { get; set; }
    }
}