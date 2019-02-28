using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace UserOperations.Models
{
    /// <summary>
    /// Information about transactions
    /// </summary>
    public class Transaction
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        public Guid TransactionId { get; set; }
        /// <summary>
        /// Amount of funds transferred
        /// </summary>
        public Decimal Amount { get; set; }
        /// <summary>
        /// Order id
        /// </summary>
        public string OrderId { get; set; }
        /// <summary>
        /// Payment id
        /// </summary>
        public string PaymentId { get; set; }
        /// <summary>
        /// Tariff id
        /// </summary>
        public Guid? TariffId { get; set; }
        public  Tariff Tariff { get; set; }
        /// <summary>
        /// Transaction status
        /// </summary>
        public int? StatusId { get; set; }
        public  Status Status { get; set; }
        /// <summary>
        /// Payment date
        /// </summary>
        public DateTime PaymentDate { get; set; }
        /// <summary>
        /// Comment
        /// </summary>
        public string TransactionComment { get; set; }
    }
}
