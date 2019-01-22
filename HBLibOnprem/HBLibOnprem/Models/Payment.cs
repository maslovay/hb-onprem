

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PostgreSQL.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }

        //компания оплаты
        public int? CompanyId { get; set; }
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
