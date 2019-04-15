using System;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
	/// <summary>
	///     Information about dialogue tone emotions in each interval
	/// </summary>
	public class DialogueInterval
    {
	    /// <summary>
	    ///     Dialogue interval id
	    /// </summary>
	    [Key]
        public Guid DialogueIntervalId { get; set; }

	    /// <summary>
	    ///     Dialogue id
	    /// </summary>
	    public Guid? DialogueId { get; set; }

        public Dialogue Dialogue { get; set; }

        /// <summary>
        ///     Is client or employee
        /// </summary>
        public Boolean IsClient { get; set; }

        /// <summary>
        ///     Beginning time of interval
        /// </summary>
        public DateTime BegTime { get; set; }

        /// <summary>
        ///     Ending time of interval
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        ///     Netrality tone share
        /// </summary>
        public Double? NeutralityTone { get; set; }

        /// <summary>
        ///     Happiness tone share
        /// </summary>
        public Double? HappinessTone { get; set; }

        /// <summary>
        ///     Saddness tone share
        /// </summary>
        public Double? SadnessTone { get; set; }

        /// <summary>
        ///     Anger tone share
        /// </summary>
        public Double? AngerTone { get; set; }

        /// <summary>
        ///     Fear tone share
        /// </summary>
        public Double? FearTone { get; set; }
    }
}