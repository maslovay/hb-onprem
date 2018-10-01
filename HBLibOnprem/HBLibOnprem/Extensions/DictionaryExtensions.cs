﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace HBLib.Extensions {
    /// <summary>
    /// Represents a collection of useful extenions methods.
    /// </summary>
    public static class DictionaryExtensions {
        /// <summary>
        /// Translate a dictionary into a string for display.
        /// </summary>
        public static string PrettyPrint<K, V>(this IDictionary<K, V> dict) {
            if (dict == null)
                return "";
            string dictStr = "[";
            ICollection<K> keys = dict.Keys;
            int i = 0;
            foreach (K key in keys) {
                dictStr += key.ToString() + "=" + dict[key].ToString();
                if (i++ < keys.Count - 1) {
                    dictStr += ", ";
                }
            }
            return dictStr + "]";
        }

        public static string JsonPrint<K, V>(this IDictionary<K, V> dict, bool indented=true) {
            if (dict == null)
                return "";
            string jsStr = "";
            if (indented) {
                jsStr = JsonConvert.SerializeObject(dict, Formatting.Indented);
            } else {
                jsStr = JsonConvert.SerializeObject(dict);
            }
           return jsStr;
        }
        public static V SetDefault<K, V>(this IDictionary<K, V> dict, K key, V @default) {
            V value;
            if (!dict.TryGetValue(key, out value)) {
                dict.Add(key, @default);
                return @default;
            } else {
                return value;
            }
        }

        public static V Pop<K, V>(this Dictionary<K, V> dict, K key) {
            V val = dict[key];
            dict.Remove(key);
            return val;
        }

        public static TValue Get<TKey, TValue>(
    this IDictionary<TKey, TValue> dict,
    TKey key,
    TValue defaultValue) {
            TValue value;
            if (key == null || !dict.TryGetValue(key, out value))
                return defaultValue;
            return value;
        }

        public static void AddRange<TKey, TValue>(this Dictionary<TKey, TValue> dict, Dictionary<TKey, TValue> collection) {
            if (collection == null) {
                throw new ArgumentNullException("Collection is null");
            }

            foreach (var entry in collection) {
                dict[entry.Key] = entry.Value;
            }

        }
    }
}