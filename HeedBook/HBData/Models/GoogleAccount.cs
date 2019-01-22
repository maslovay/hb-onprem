using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HBData.Models
{
    public class GoogleAccount
    {
        [Key]
        public Guid GoogleAccountId { get; set; }
        public string GoogleKey { get; set; }
        public int StatusId { get; set; }
        public virtual Status Status { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime ExpirationTime { get; set; }
    }
}
