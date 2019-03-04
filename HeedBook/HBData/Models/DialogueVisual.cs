using System;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    /// <summary>
    /// Average dialogue client emotions
    /// </summary>
    public class DialogueVisual
    {
        /// <summary>
        /// Dialogue visual id
        /// </summary>
        [Key]
        public Guid DialogueVisualId { get; set; }
		/// <summary>
        /// Dialogue id
        /// </summary>
		public Guid? DialogueId { get; set; }
        public  Dialogue Dialogue { get; set; }
        /// <summary>
        /// Average attention share
        /// </summary>
        public double? AttentionShare { get; set; }
        /// <summary>
        /// Average dialogue happiness share
        /// </summary>
        public double? HappinessShare { get; set; }
        /// <summary>
        /// Average dialogue neutral share
        /// </summary>
        public double? NeutralShare { get; set; }
        /// <summary>
        /// Average dialogue surprise share
        /// </summary>
        public double? SurpriseShare { get; set; }
        /// <summary>
        /// Average dialogue sadness share
        /// </summary>
        public double? SadnessShare { get; set; }
        /// <summary>
        /// Average dialogue anger share
        /// </summary>
        public double? AngerShare { get; set; }
        /// <summary>
        /// Average dialogue disgust share
        /// </summary>
        public double? DisgustShare { get; set; }
        /// <summary>
        /// Average dialogue disgust share
        /// </summary>
        public double? ContemptShare { get; set; }
        /// <summary>
        /// Average dialogue fear share
        /// </summary>
        public double? FearShare { get; set; }
		
    }
}
