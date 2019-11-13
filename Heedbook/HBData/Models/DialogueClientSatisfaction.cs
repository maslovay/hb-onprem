using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace HBData.Models
{
    /// <summary>
    ///     Information about intergral score
    /// </summary>
    public class DialogueClientSatisfaction
    {
        /// <summary>
        ///     Id of satisfaction score
        /// </summary>
        [Key]
        public Guid DialogueClientSatisfactionId { get; set; }

        /// <summary>
        ///     Dialogue id
        /// </summary>
        public Guid? DialogueId { get; set; }
        [JsonIgnore]
        public Dialogue Dialogue { get; set; }

        /// <summary>
        ///     Total dialogue estimation
        /// </summary>
        public Double? MeetingExpectationsTotal { get; set; }

        /// <summary>
        ///     Early total mood of client
        /// </summary>
        public Double? BegMoodTotal { get; set; }

        /// <summary>
        ///     Later total moode of client
        /// </summary>
        public Double? EndMoodTotal { get; set; }

        /// <summary>
        ///     Dialogue satisfaction estimation by client
        /// </summary>
        public Double? MeetingExpectationsByClient { get; set; }

        /// <summary>
        ///     Dialogue satisfaction estimation by employee
        /// </summary>
        public Double? MeetingExpectationsByEmpoyee { get; set; }

        /// <summary>
        ///     Dialogue early satisfaction estimation by employee
        /// </summary>
        public Double? BegMoodByEmpoyee { get; set; }

        /// <summary>
        ///     Dialogue later satisfaction estimation by employee
        /// </summary>
        public Double? EndMoodByEmpoyee { get; set; }

        /// <summary>
        ///     Dialogue total satisfaction estimation by teacher
        /// </summary>
        public Double? MeetingExpectationsByTeacher { get; set; }

        /// <summary>
        ///     Dialogue early satisfaction estimation by teacher
        /// </summary>
        public Double? BegMoodByTeacher { get; set; }

        /// <summary>
        ///     Dialogue later satisfaction estimation by teacher
        /// </summary>
        public Double? EndMoodByTeacher { get; set; }

        /// <summary>
        ///     Dialogue total satisfaction estimation by nn
        /// </summary>
        public Double? MeetingExpectationsByNN { get; set; }

        /// <summary>
        ///     Dialogue early satisfaction estimation by nn
        /// </summary>
        public Double? BegMoodByNN { get; set; }

        /// <summary>
        ///     Dialogue later satisfaction estimation by nn
        /// </summary>
        public Double? EndMoodByNN { get; set; }
        /// <summary>
        ///    Client age in dialogue
        /// </summary>
        public Double? Age { get; set; }
        /// <summary>
        ///     Client gender in dialogue -"male" : "female"
        /// </summary>
        public string Gender { get; set; }
    }
}