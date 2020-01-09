using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace HBData.Models
{
    /// <summary>
    ///     Information about device
    /// </summary>
    public class Device
    {
        /// <summary>
        ///     Device id
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid DeviceId { get; set; }

        /// <summary>
        ///     generated device password(at the front) 6 characters long from the set of all Latin letters and numbers
        /// </summary>
        [Required]
        public String Code { get; set; }

        /// <summary>
        ///     Device name
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        ///     Id of company
        /// </summary>
        public Guid CompanyId { get; set; }
        [JsonIgnore] public Company Company { get; set; }

        /// <summary>
        ///     Id of device type
        /// </summary>
        public Guid? DeviceTypeId { get; set; }
        [JsonIgnore] public DeviceType DeviceType { get; set; }

        /// <summary>
        ///     Device status - active / inactive
        /// </summary>
        public Int32? StatusId { get; set; }
        [JsonIgnore] public Status Status { get; set; }

        /// <summary>
        ///     Link to dialogues from this device
        /// </summary>
        [JsonIgnore] public ICollection<Dialogue> Dialogues { get; set; }

        /// <summary>
        ///     Link to sessions from this device
        /// </summary>
        [JsonIgnore] public ICollection<Session> Sessions { get; set; }

        /// <summary>
        ///     Link to content shown sessions from this device
        /// </summary>
        [JsonIgnore] public ICollection<SlideShowSession> SlideShowSessions { get; set; }

        /// <summary>
        ///     Link to answers on poll shown from this device
        /// </summary>
        [JsonIgnore] public ICollection<CampaignContentAnswer> CampaignContentAnswers { get; set; }
    }
}