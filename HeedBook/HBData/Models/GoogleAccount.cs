using System;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    /// <summary>
    /// Keys for google account
    /// </summary>
    public class GoogleAccount
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        public Guid GoogleAccountId { get; set; }
        /// <summary>
        /// Google key
        /// </summary>
        public string GoogleKey { get; set; }
        /// <summary>
        ///  Google key status id
        /// </summary>
        public int? StatusId { get; set; }
        public Status Status { get; set; }
        /// <summary>
        /// Google key creation date
        /// </summary>
        public DateTime CreationTime { get; set; }
        /// <summary>
        /// Google key expiration date
        /// </summary>
        public DateTime ExpirationTime { get; set; }
    }
}
