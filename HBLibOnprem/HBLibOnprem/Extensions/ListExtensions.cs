using Newtonsoft.Json;
using System.Collections.Generic;

namespace HBLib.Extensions {
    /// <summary>
    /// Represents a collection of useful extenions methods.
    /// </summary>
    public static class ListExtensions {
            public static string JsonPrint<T>(this ICollection<T> lst) {
                //logic for moving the item goes here.
                if (lst == null)
                    return "";
                var jsStr = JsonConvert.SerializeObject(lst, Formatting.Indented);
                return jsStr;
            }
    }
}