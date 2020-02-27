using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Old.Models
{
    /// <summary>
    ///     Information about tariffs
    /// </summary>
    public class Tariff
    {
        /// <summary>
        ///     Id
        /// </summary>
        [Key]
        public Guid TariffId { get; set; }

        /// <summary>
        ///     Company with tariff
        /// </summary>
        public Guid? CompanyId { get; set; }

        public Company Company { get; set; }

        /// <summary>
        ///     Customer key for payments.
        /// </summary>
        public String CustomerKey { get; set; }

        /// <summary>
        ///     Cost
        /// </summary>
        public Decimal TotalRate { get; set; }

        /// <summary>
        ///     Employee number in tariff
        /// </summary>
        public Int32 EmployeeNo { get; set; }

        /// <summary>
        ///     Tariff creation time
        /// </summary>
        public DateTime CreationDate { get; set; }

        /// <summary>
        ///     Tariff expiration date
        /// </summary>
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        ///     Code for recurrent payments
        /// </summary>
        public String Rebillid { get; set; }

        /// <summary>
        ///     Token for recurrent payments
        /// </summary>
        public Byte[] Token { get; set; }

        /// <summary>
        ///     Tariff status
        /// </summary>
        public Int32? StatusId { get; set; }

        public Status Status { get; set; }

        /// <summary>
        ///     Is monthly or annual tariff
        /// </summary>
        public Boolean isMonthly { get; set; }

        /// <summary>
        ///     Comment
        /// </summary>
        public String TariffComment { get; set; }
        /// <summary>
        ///     all Transactions attached to tariff
        /// </summary>
        public ICollection<Transaction> Transactions { get; set; }
    }
}