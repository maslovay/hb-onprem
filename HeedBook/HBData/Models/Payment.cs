using System;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    /// <summary>
    /// Payments
    /// </summary>
    public class Payment
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        public Guid PaymentId { get; set; }
        /// <summary>
        /// Company id
        /// </summary>
        public Guid? CompanyId { get; set; }
        public  Company Company { get; set; }
        /// <summary>
        /// Payment status
        /// </summary>
        public int? StatusId { get; set; }
        public  Status Status { get; set; }
        /// <summary>
        /// Payment date
        /// </summary>
        public DateTime Date { get; set; }
        /// <summary>
        /// Transaction id
        /// </summary>
        public string TransactionId { get; set; }
        /// <summary>
        /// Amount of payment
        /// </summary>
        public double PaymentAmount { get; set; }
        /// <summary>
        /// Paid time
        /// </summary>
        public double PaymentTime { get; set; }
        /// <summary>
        /// Comment about payment
        /// </summary>
        public string PaymentComment { get; set; }

    }
}
