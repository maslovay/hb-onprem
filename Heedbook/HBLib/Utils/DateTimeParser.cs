using System;
using System.Globalization;

namespace HBLib.Utils
{
    public static class DateTimeParser
    {
        public static DateTime ParseAsUTC(String timeValue, String timeType = null)
        {
            if (timeType == "System")
                return DateTime.ParseExact(timeValue, "yyyyMMddHHmmss", CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal);
            if (timeType == "Mongo")
                return DateTime.ParseExact(timeValue, "yyyyMMddHHmmss.fff", CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal);
            return DateTime.ParseExact(timeValue, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal);
        }


        public static String DateTimeToString(DateTime dt, Boolean isSystem = true)
        {
            if (isSystem)
                return dt.ToString("yyyyMMddHHmmss");
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static String DateToString(DateTime dt, Boolean isSystem = true)
        {
            if (isSystem)
                return dt.ToString("yyyyMMdd");
            return dt.ToString("yyyy-MM-dd");
        }

        public static String TimeToString(DateTime dt, Boolean isSystem = true)
        {
            if (isSystem)
                return dt.ToString("HHmmss");
            return dt.ToString("HH:mm:ss");
        }

        public static DateTime Parse(String s, String format)
        {
            return DateTime.ParseExact(s, format, CultureInfo.InvariantCulture.DateTimeFormat);
        }
    }
}