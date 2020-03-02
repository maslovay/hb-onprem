using System;
using System.Globalization;

namespace HBLib.Utils
{
    public class DT
    {
        public static String Format(DateTime dt, Boolean isSystem = true)
        {
            if (isSystem)
                return dt.ToString("yyyyMMddHHmmss");
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static String FormatDate(DateTime dt, Boolean isSystem = true)
        {
            if (isSystem)
                return dt.ToString("yyyyMMdd");
            return dt.ToString("yyyy-MM-dd");
        }

        public static String FormatTime(DateTime dt, Boolean isSystem = true)
        {
            if (isSystem)
                return dt.ToString("HHmmss");
            return dt.ToString("HH:mm:ss");
        }

        public static DateTime Parse(String s, Boolean isSystem = true)
        {
            if (isSystem)
                return Parse(s, "yyyyMMddHHmmss");
            return Parse(s, "yyyy-MM-dd HH:mm:ss");
        }

        public static DateTime Parse(String s, String format)
        {
            return DateTime.ParseExact(s, format, CultureInfo.InvariantCulture.DateTimeFormat);
        }
    }
}