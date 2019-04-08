using System;
using System.Globalization;

namespace HBLib.Utils
{
    public class DT
    {
        public static string Format(DateTime dt, bool isSystem = true)
        {
            if (isSystem)
                return dt.ToString("yyyyMMddHHmmss");
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static string FormatDate(DateTime dt, bool isSystem = true)
        {
            if (isSystem)
                return dt.ToString("yyyyMMdd");
            return dt.ToString("yyyy-MM-dd");
        }

        public static string FormatTime(DateTime dt, bool isSystem = true)
        {
            if (isSystem)
                return dt.ToString("HHmmss");
            return dt.ToString("HH:mm:ss");
        }

        public static DateTime Parse(string s, bool isSystem = true)
        {
            if (isSystem)
                return Parse(s, "yyyyMMddHHmmss");
            return Parse(s, "yyyy-MM-dd HH:mm:ss");
        }

        public static DateTime Parse(string s, string format)
        {
            return DateTime.ParseExact(s, format, CultureInfo.InvariantCulture.DateTimeFormat);
        }
    }
}