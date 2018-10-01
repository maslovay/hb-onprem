using HBLib.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HBLib.Utils {
    public class Timer {
        /*Sample usage:             
         *  var cls = new Timer("timer_cache.json");
            cls.Reset();
            cls.Clock("b");
            Thread.Sleep(500);
            cls.Clock("b");
            cls.PrintStats();
            Console.ReadLine();
        */
        public Dictionary<string, DateTime> checkpoints;
        public Dictionary<string, List<double>> laps;
        public string cacheFn;
        public bool useCache;
        public bool dumpCacheContinously;
        public Timer(string cacheFn = null, bool reset = false, bool dumpCacheContinously = true) {
            this.checkpoints = new Dictionary<string, DateTime>();
            this.laps = new Dictionary<string, List<double>>();
            this.cacheFn = cacheFn;
            this.useCache = this.cacheFn != null;
            this.LoadCache();
            this.dumpCacheContinously = dumpCacheContinously;
            if (reset) {
                this.Reset();
            }
        }

        public void Clock(string key) {
            var t = DateTime.Now;
            if (!this.checkpoints.ContainsKey(key)) {
                this.checkpoints[key] = t;
            } else {
                var t0 = this.checkpoints.Pop(key);
                this.laps.SetDefault(key, new List<double>()).Add((t - t0).TotalSeconds);
                if (this.dumpCacheContinously) {
                    this.DumpCache();
                }
            }
        }
        public void Reset() {
            this.checkpoints = new Dictionary<string, DateTime>();
            this.laps = new Dictionary<string, List<double>>();
            this.DumpCache();
        }
        public void LoadCache() {
            if (!File.Exists(this.cacheFn) | !this.useCache) {
                return;
            }

            using (StreamReader r = new StreamReader(this.cacheFn)) {
                string json = r.ReadToEnd();
                var cache = JsonConvert.DeserializeObject<Cache>(json);
                this.laps = cache.laps;
            }

        }
        public Dictionary<string, Dictionary<string, double>> GetStats() {
            var res = new Dictionary<string, Dictionary<string, double>>();

            foreach (var entry in this.laps) {
                var curStats = new Dictionary<string, double>();

                curStats["Average"] = entry.Value.Average();
                curStats["Count"] = entry.Value.Count();
                curStats["Max"] = entry.Value.Max();
                curStats["Min"] = entry.Value.Min();
                curStats["Sum"] = entry.Value.Sum();

                res[entry.Key] = curStats;
            }
            return res;
        }
        public Dictionary<string, Dictionary<string, string>> GetStringStats() {
            var stats = this.GetStats();
            var stringStats = new Dictionary<string, Dictionary<string, string>>();

            foreach (var entry1 in stats) {

                var curStats = new Dictionary<string, string>();

                foreach (var entry2 in stats[entry1.Key]) {
                    curStats[entry2.Key] = string.Format("{0:N2}", entry2.Value);
                }

                stringStats[entry1.Key] = curStats;

            }
            return stringStats;
        }

        public void DumpCache() {
            if (!this.useCache) {
                return;
            }
            var cache = new Cache();
            cache.laps = this.laps;
            cache.stats = this.GetStats();

            string json = JsonConvert.SerializeObject(cache, Formatting.Indented);
            using (StreamWriter w = new StreamWriter(this.cacheFn)) {
                w.Write(json);
            }
        }

        public void PrintStats() {
            var stringStats = this.GetStringStats();
            string json = JsonConvert.SerializeObject(stringStats, Formatting.Indented);
            Console.WriteLine(json);
        }

        class Cache {
            public Dictionary<string, List<double>> laps;
            public Dictionary<string, Dictionary<string, double>> stats;
        }

    }
}