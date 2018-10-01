using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBLib.Utils
{
    public static class DateTimeParser
    {
        public static DateTime ParseAsUTC(string timeValue, string timeType = null)
        {
            if (timeType == "System")
            {
                return DateTime.ParseExact(timeValue, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            }
            if (timeType == "Mongo")
            {
                return DateTime.ParseExact(timeValue, "yyyyMMddHHmmss.fff", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            }
            else
            {
                return DateTime.ParseExact(timeValue, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            }
        }


        public static string DateTimeToString (DateTime dt, bool isSystem = true)
        {
            if (isSystem)
            {
                return dt.ToString("yyyyMMddHHmmss");
            }
            else
            {
                return dt.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }

        public static string DateToString(DateTime dt, bool isSystem = true)
        {
            if (isSystem)
            {
                return dt.ToString("yyyyMMdd");
            }
            else
            {
                return dt.ToString("yyyy-MM-dd");
            }
        }
        public static string TimeToString(DateTime dt, bool isSystem = true)
        {
            if (isSystem)
            {
                return dt.ToString("HHmmss");
            }
            else
            {
                return dt.ToString("HH:mm:ss");
            }
        }

        public static DateTime Parse(string s, string format)
        {
            return DateTime.ParseExact(s, format, CultureInfo.InvariantCulture.DateTimeFormat);
        }
    }
}
