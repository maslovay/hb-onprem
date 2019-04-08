// using System;
// using System.Collections.Generic;
// using System.Collections.Specialized;
// using System.Linq;
// using System.Web;
// using Microsoft.Extensions.Primitives;

// namespace HBLib.Utils
// {
//     public class HttpQuery
//     {
//         /*Usage:
//           var par = new HttpQuery();

//             var url = "https://sample_url?key1=val1&key2=val2.1&key2=val2.2&key3=";

//             // add string url
//             par.Add(url);
//             // add manually
//             par["key4"] = "a"; // or par.Add("key4", "a")
            
//             // add params in af: par.Add(req.GetQueryNameValuePairs());

//             Console.WriteLine(par.ToDictionary().JsonPrint());
//             {
//               "key1": [
//                 "val1"
//               ],
//               "key2": [
//                 "val2.1",
//                 "val2.2"
//               ],
//               "key3": [
//                 ""
//               ],
//               "key4": [
//                 "a"
//               ]
//             }

//         // get string 
//         par["key1"].ToString(); // "val1"
//         par["key2"].ToList(); // ["val2.1", "val2.2"]
//         par["key2"].ToString(); // "val2.1,val2.2"
//         par["key3"].ToString(); // ""
//         par["key3"].ToList(); // [""]

//         // working with non-obligatory parameters
//         //par["non_existing_key"].ToString(); // Exception: key does not exist!
//         par.Get("non_existing_key").ToString(); // ""
//         par.Get("non_existing_key").ToList(); // []

//         // generate query
//         par.ToQueryString(); // ?key1=val1&key2=val2.1&key2=val2.2&key3=&key4=a

//             */

//         private readonly Dictionary<string, StringValues> par;

//         public HttpQuery()
//         {
//             par = new Dictionary<string, StringValues>();
//         }

//         public StringValues this[string index]
//         {
//             get => par[index];
//             set => Add(index, value);
//         }

//         public void Add(string key, string value)
//         {
//             // par.SetDefault(key, new StringValues());
//             var newValues = par[key].ToList();
//             newValues.Add(value);
//             par[key] = new StringValues(newValues.ToArray());
//         }

//         public void Add(string key, List<string> values)
//         {
//             foreach (var value in values) Add(key, value);
//         }

//         public void Add(string url)
//         {
//             var uri = new Uri(url);
//             var parsed = HttpUtility.ParseQueryString(uri.Query);
//             foreach (var k in parsed.AllKeys)
//             foreach (var s in parsed.GetValues(k))
//                 Add(k, s);
//         }

//         public void Add(IEnumerable<KeyValuePair<string, string>> nvc)
//         {
//             foreach (var kv in nvc) Add(kv.Key, kv.Value);
//         }

//         private string ToQueryString()
//         {
//             var nvc = new NameValueCollection();
//             foreach (var kv in par)
//             foreach (var s in kv.Value)
//                 nvc.Add(kv.Key, s);
//             var array = (from key in nvc.AllKeys
//                     from value in nvc.GetValues(key)
//                     select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)))
//                 .ToArray();
//             return "?" + string.Join("&", array);
//         }

//         public StringValues Get(StringValues key, StringValues d = new StringValues())
//         {
//             if (!par.ContainsKey(key))
//                 return d;
//             return par[key];
//         }

//         public Dictionary<string, StringValues> ToDictionary()
//         {
//             return par;
//         }
//     }
// }