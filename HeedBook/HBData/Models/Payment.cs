

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HBData.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        //компания оплаты
        public Guid? CompanyId { get; set; }
        public  Company Company { get; set; }


        //status of payment
        public int? StatusId { get; set; }
        public  Status Status { get; set; }

        //дата оплаты
        public DateTime Date { get; set; }

        //transaction Id
        public string TransactionId { get; set; }

        //payment amount summ
        public double PaymentAmount { get; set; }

        //оплаченное время, мин
        public double PaymentTime { get; set; }

        //комментарий по оплате
        public string PaymentComment { get; set; }

    }
}
