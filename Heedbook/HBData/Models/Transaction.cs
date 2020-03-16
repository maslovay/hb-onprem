using System;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    /// <summary>
    ///     Information about transactions
    /// </summary>
    public class Transaction
    {
        /// <summary>
        ///     Id
        /// </summary>
        [Key]
        public Guid TransactionId { get; set; }

        /// <summary>
        ///     Amount of funds transferred
        /// </summary>
        public Decimal Amount { get; set; }

        /// <summary>
        ///     Order id
        /// </summary>
        public String OrderId { get; set; }

        /// <summary>
        ///     Payment id
        /// </summary>
        public String PaymentId { get; set; }

        /// <summary>
        ///     Tariff id
        /// </summary>
        public Guid? TariffId { get; set; }

        public Tariff Tariff { get; set; }

        /// <summary>
        ///     Transaction status
        /// </summary>
        public Int32? StatusId { get; set; }

        public Status Status { get; set; }

        /// <summary>
        ///     Payment date
        /// </summary>
        public DateTime PaymentDate { get; set; }

        /// <summary>
        ///     Comment
        /// </summary>
        public String TransactionComment { get; set; }
    }
}