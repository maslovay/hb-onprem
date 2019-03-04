using System;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    /// <summary>
    /// Information about intergral score
    /// </summary>
    public class DialogueClientSatisfaction
    {
        /// <summary>
        /// Id of satisfaction score
        /// </summary>
        [Key]
        public Guid DialogueClientSatisfactionId { get; set; }
        /// <summary>
        /// Dialogue id
        /// </summary>
        public Guid? DialogueId { get; set; }
        public  Dialogue Dialogue { get; set; }
        /// <summary>
        /// Total dialogue estimation
        /// </summary>
        public double? MeetingExpectationsTotal { get; set; }
        /// <summary>
        /// Early total mood of client
        /// </summary>
        public double? BegMoodTotal { get; set; }
        /// <summary>
        /// Later total moode of client
        /// </summary>
        public double? EndMoodTotal { get; set; }
        /// <summary>
        /// Dialogue satisfaction estimation by client
        /// </summary>
        public double? MeetingExpectationsByClient { get; set; }
        /// <summary>
        /// Dialogue satisfaction estimation by employee
        /// </summary>
        public double? MeetingExpectationsByEmpoyee { get; set; }
        /// <summary>
        /// Dialogue early satisfaction estimation by employee
        /// </summary>
        public double? BegMoodByEmpoyee { get; set; }
        /// <summary>
        /// Dialogue later satisfaction estimation by employee
        /// </summary>
        public double? EndMoodByEmpoyee { get; set; }
        /// <summary>
        /// Dialogue total satisfaction estimation by teacher
        /// </summary>
        public double? MeetingExpectationsByTeacher { get; set; }
        /// <summary>
        /// Dialogue early satisfaction estimation by teacher
        /// </summary>
        public double? BegMoodByTeacher { get; set; }
        /// <summary>
        /// Dialogue later satisfaction estimation by teacher
        /// </summary>
        public double? EndMoodByTeacher { get; set; }
        /// <summary>
        /// Dialogue total satisfaction estimation by nn
        /// </summary>
        public double? MeetingExpectationsByNN { get; set; }
        /// <summary>
        /// Dialogue early satisfaction estimation by nn
        /// </summary>
        public double? BegMoodByNN { get; set; }
        /// <summary>
        /// Dialogue later satisfaction estimation by nn
        /// </summary>
        public double? EndMoodByNN { get; set; }
    }
}
