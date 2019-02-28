using System.Collections.Generic;
using System.Dynamic;

namespace HBLib.Utils
{
    public static class ServiceClass
    {
        public static bool IsPropertyExist(dynamic settings, string name)
        {
            if (settings is ExpandoObject)
                return ((IDictionary<string, object>) settings).ContainsKey(name);

            return settings.GetType().GetProperty(name) != null;
        }
    }
}