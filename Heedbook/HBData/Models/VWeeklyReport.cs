using System;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    /// <summary>
    ///     View table
    /// </summary>
    public class VWeeklyReport
    {
        [Key]
        public Guid Id { get; set; }
        /// <summary>
        ///     Period in days
        /// </summary>
        public DateTime Day { get; set; }
        public Guid AspNetUserId { get; set; }
        public int Dialogues { get; set; }
        public double? DialogueHours { get; set; }
        public double? Satisfaction { get; set; }
        public double? PositiveEmotions { get; set; }
        public double? PositiveTone { get; set; }
        public double? SpeekEmotions { get; set; }
        public int? CrossDialogues { get; set; }
        public int? NecessaryDialogues { get; set; }
        public int? LoyaltyDialogues { get; set; }
        public int? AlertDialogues { get; set; }
        public int? FillersDialogues { get; set; }
    }
}