using System;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    /// <summary>
    /// Information about tariffs
    /// </summary>
    public class Tariff
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        public Guid TariffId { get; set; }
        /// <summary>
        /// Company with tariff
        /// </summary>
        public Guid? CompanyId { get; set; }
        public  Company Company { get; set; }
        /// <summary>
        /// Customer key for payments. 
        /// </summary>
        public string CustomerKey { get; set; }
        /// <summary>
        /// Cost
        /// </summary>
        public Decimal TotalRate { get; set; }
        /// <summary>
        /// Employee number in tariff
        /// </summary>
        public int EmployeeNo { get; set; }
        /// <summary>
        /// Tariff creation time
        /// </summary>
        public DateTime CreationDate { get; set; }
        /// <summary>
        /// Tariff expiration date
        /// </summary>
        public DateTime ExpirationDate { get; set; }
        /// <summary>
        /// Code for recurrent payments
        /// </summary>
        public string Rebillid { get; set; }
        /// <summary>
        /// Token for recurrent payments
        /// </summary>
        public byte[] Token { get; set; }
        /// <summary>
        /// Tariff status 
        /// </summary>
        public int? StatusId { get; set; }
        public  Status Status { get; set; }
        /// <summary>
        /// Is monthly or annual tariff
        /// </summary>        
        public bool isMonthly { get; set; }
        /// <summary>
        /// Comment
        /// </summary>
        public string TariffComment { get; set; }
    }
}
