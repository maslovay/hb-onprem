using System;

namespace HBLib.Utils {
    public class DT
    {
        public static string Format(DateTime dt, bool isSystem=true) {
            if (isSystem) {
                return dt.ToString("yyyyMMddHHmmss");
            } else {
                return dt.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }

        public static string FormatDate(DateTime dt, bool isSystem = true) {
            if (isSystem) {
                return dt.ToString("yyyyMMdd");
            } else {
                return dt.ToString("yyyy-MM-dd");
            }
        }
        public static string FormatTime(DateTime dt, bool isSystem = true) {
            if (isSystem) {
                return dt.ToString("HHmmss");
            } else {
                return dt.ToString("HH:mm:ss");
            }
        }

        public static DateTime Parse(string s, bool isSystem = true) {
            if (isSystem) {
                return Parse(s, "yyyyMMddHHmmss");
            } else {
                return Parse(s, "yyyy-MM-dd HH:mm:ss");
            }
        }

        public static DateTime Parse(string s, string format) {
            return DateTime.ParseExact(s, format, System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat);
        }
    }
}
