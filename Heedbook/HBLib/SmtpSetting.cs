using System;

namespace HBLib
{
    public class SmtpSettings
    {
        public String Host { get; set; }

        public Int32 Port { get; set; }

        public String FromEmail { get; set; }

        public String Password { get; set; }

        public String ToEmail { get; set; }

        public Int32 DeliveryMethod { get; set; }

        public Boolean EnableSsl {get; set;}

        public Boolean UseDefaultCredentials {get; set;}

        public Int32 Timeout {get; set;}
    }
}