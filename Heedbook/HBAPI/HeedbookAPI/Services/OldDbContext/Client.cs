using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Old.Models
{
    /// <summary>
    ///     Clients are the unique persons present in the dialogs
    ///     Client created 1. When a dialog appears with the client. 2. When sending a photo. (will be implemented later)
    /// </summary>
    public class Client
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
       // [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ClientId { get; set; }

        /// <summary>
        /// client's name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// client’s phone
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// client’s e-mail
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// client’s gender
        /// </summary>
        public string Gender { get; set; }//male-female

        public int Age { get; set; }

        /// <summary>
        /// vector facial signs -embedding
        /// </summary>
        public double[] FaceDescriptor { get; set; }

        /// <summary>
        /// link to the photo of the client’s profile picture, photo from the first dialogue of this client
        /// </summary>
        public string Avatar { get; set; }

        /// <summary>
        /// Client status id (active, inactive...)
        /// </summary>
        public Int32? StatusId { get; set; }
        [JsonIgnore] public Status Status { get; set; }

        /// <summary>
        /// company in which the client was
        /// </summary>
        public Guid CompanyId { get; set; }
        [JsonIgnore] public Company Company { get; set; }

        /// <summary>
        /// the corporation the company the client was in
        /// </summary>
        public Guid? CorporationId { get; set; }
        [JsonIgnore] public Corporation Corporation { get; set; }

        /// <summary>
        /// collection of dialogues where client appeared
        /// </summary>
        [JsonIgnore] public ICollection<Dialogue> Dialogues { get; set; }
        /// <summary>
        /// collection of notes wich user make about client
        /// </summary>
        [JsonIgnore] public ICollection<ClientNote> ClientNotes { get; set; }
    }
}