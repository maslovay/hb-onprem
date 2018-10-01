using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HBLib.Models
{
    public class Transaction
    {
        public Guid TransactionId { get; set; }

        //оплата
        public Decimal Amount { get; set; }

        //id заказа в системе продавца (см. API банка Тинькофф)
        public string OrderId { get; set; }

        //id транзакции в системе банка (см. API банка Тинькофф)
        public string PaymentId { get; set; }

        //тариф, по которому произошла оплата
        public Guid? TariffId { get; set; }
        public virtual Tariff Tariff { get; set; }

        //статус транзакции
        public int? StatusId { get; set; }
        public virtual Status Status { get; set; }

        //дата оплаты
        public DateTime PaymentDate { get; set; }

        //комментарий к оплате
        public string TransactionComment { get; set; }
    }
}
