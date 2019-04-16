using System;

namespace HBData.Models.AccountViewModels
{
    public class AccountAuthorization
    {
        public String UserName { get; set; }

        public String Password { get; set; }

        public Boolean Remember { get; set; }
    }
}