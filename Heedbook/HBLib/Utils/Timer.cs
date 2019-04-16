// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using HBLib.Extensions;
// using Newtonsoft.Json;

// namespace HBLib.Utils
// {
//     public class Timer
//     {
//         public string cacheFn;

//         /*Sample usage:             
//          *  var cls = new Timer("timer_cache.json");
//             cls.Reset();
//             cls.Clock("b");
//             Thread.Sleep(500);
//             cls.Clock("b");
//             cls.PrintStats();
//             Console.ReadLine();
//         */
//         public Dictionary<string, DateTime> checkpoints;
//         public bool dumpCacheContinously;
//         public Dictionary<string, List<double>> laps;
//         public bool useCache;

//         public Timer(string cacheFn = null, bool reset = false, bool dumpCacheContinously = true)
//         {
//             checkpoints = new Dictionary<string, DateTime>();
//             laps = new Dictionary<string, List<double>>();
//             this.cacheFn = cacheFn;
//             useCache = this.cacheFn != null;
//             LoadCache();
//             this.dumpCacheContinously = dumpCacheContinously;
//             if (reset) Reset();
//         }

//         public void Clock(string key)
//         {
//             var t = DateTime.Now;
//             if (!checkpoints.ContainsKey(key))
//             {
//                 checkpoints[key] = t;
//             }
//             else
//             {
//                 var t0 = checkpoints.Pop(key);
//                 laps.SetDefault(key, new List<double>()).Add((t - t0).TotalSeconds);
//                 if (dumpCacheContinously) DumpCache();
//             }
//         }

//         public void Reset()
//         {
//             checkpoints = new Dictionary<string, DateTime>();
//             laps = new Dictionary<string, List<double>>();
//             DumpCache();
//         }

//         public void LoadCache()
//         {
//             if (!File.Exists(cacheFn) | !useCache) return;

//             using (var r = new StreamReader(cacheFn))
//             {
//                 var json = r.ReadToEnd();
//                 var cache = JsonConvert.DeserializeObject<Cache>(json);
//                 laps = cache.laps;
//             }
//         }

//         public Dictionary<string, Dictionary<string, double>> GetStats()
//         {
//             var res = new Dictionary<string, Dictionary<string, double>>();

//             foreach (var entry in laps)
//             {
//                 var curStats = new Dictionary<string, double>();

//                 curStats["Average"] = entry.Value.Average();
//                 curStats["Count"] = entry.Value.Count();
//                 curStats["Max"] = entry.Value.Max();
//                 curStats["Min"] = entry.Value.Min();
//                 curStats["Sum"] = entry.Value.Sum();

//                 res[entry.Key] = curStats;
//             }

//             return res;
//         }

//         public Dictionary<string, Dictionary<string, string>> GetStringStats()
//         {
//             var stats = GetStats();
//             var stringStats = new Dictionary<string, Dictionary<string, string>>();

//             foreach (var entry1 in stats)
//             {
//                 var curStats = new Dictionary<string, string>();

//                 foreach (var entry2 in stats[entry1.Key]) curStats[entry2.Key] = string.Format("{0:N2}", entry2.Value);

//                 stringStats[entry1.Key] = curStats;
//             }

//             return stringStats;
//         }

//         public void DumpCache()
//         {
//             if (!useCache) return;
//             var cache = new Cache();
//             cache.laps = laps;
//             cache.stats = GetStats();

//             var json = JsonConvert.SerializeObject(cache, Formatting.Indented);
//             using (var w = new StreamWriter(cacheFn))
//             {
//                 w.Write(json);
//             }
//         }

//         public void PrintStats()
//         {
//             var stringStats = GetStringStats();
//             var json = JsonConvert.SerializeObject(stringStats, Formatting.Indented);
//             Console.WriteLine(json);
//         }

//         private class Cache
//{
//             public Dictionary<string, List<double>> laps;
//             public Dictionary<string, Dictionary<string, double>> stats;
//         }
//     }
// }

