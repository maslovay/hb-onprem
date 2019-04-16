using System;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    /// <summary>
    ///     Payments
    /// </summary>
    public class Payment
    {
        /// <summary>
        ///     Id
        /// </summary>
        [Key]
        public Guid PaymentId { get; set; }

        /// <summary>
        ///     Company id
        /// </summary>
        public Guid? CompanyId { get; set; }

        public Company Company { get; set; }

        /// <summary>
        ///     Payment status
        /// </summary>
        public Int32? StatusId { get; set; }

        public Status Status { get; set; }

        /// <summary>
        ///     Payment date
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        ///     Transaction id
        /// </summary>
        public String TransactionId { get; set; }

        /// <summary>
        ///     Amount of payment
        /// </summary>
        public Double PaymentAmount { get; set; }

        /// <summary>
        ///     Paid time
        /// </summary>
        public Double PaymentTime { get; set; }

        /// <summary>
        ///     Comment about payment
        /// </summary>
        public String PaymentComment { get; set; }
    }
}