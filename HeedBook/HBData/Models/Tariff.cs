using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HBData.Models
{
    public class Tariff
    {
        [Key]
        public Guid TariffId { get; set; }

        //компания, которой принадлежит данный тариф
        public Guid? CompanyId { get; set; }
        public  Company Company { get; set; }

        //ключ покупателя, необходим для рекуррентных платежей. Ассоциируется с банковской картой в системе Тинькова.
        public string CustomerKey { get; set; }

        //стоимость
        public Decimal TotalRate { get; set; }

        //количество сотрудников в тарифе
        public int EmployeeNo { get; set; }

        //дата начала действия тарифа
        public DateTime CreationDate { get; set; }

        //срок действия тарифа
        public DateTime ExpirationDate { get; set; }

        //код для рекуррентных платежей
        public string Rebillid { get; set; }

        //токен для проведения рекуррентного платежа
        public byte[] Token { get; set; }

        //статус тарифа
        public int? StatusId { get; set; }
        public  Status Status { get; set; }

        //Помесячная или погодовая оплата
        public bool isMonthly { get; set; }

        //Комментарий
        public string TariffComment { get; set; }
    }
}
