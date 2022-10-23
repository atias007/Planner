﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Planar.Common
{
    public static class Extensions
    {
        public static bool IsNullOrEmpty<T>(this List<T> list)
        {
            return list == null || list.Count == 0;
        }

        public static bool NotContains<T>(this IEnumerable<T> list, T value)
        {
            return list == null || !list.Contains(value);
        }

        public static bool NotContainsKey<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
        {
            return dictionary == null || !dictionary.ContainsKey(key);
        }

        public static bool NotContainsKey<TValue>(this Dictionary<string, TValue> dictionary, string key, bool ignoreCase)
        {
            if (dictionary == null) { return true; }
            if (string.IsNullOrEmpty(key)) { return true; }

            if (ignoreCase)
            {
                return !dictionary.Keys.Any(k => k.ToLower() == key.ToLower());
            }
            else
            {
                return !dictionary.ContainsKey(key);
            }
        }

        public static TValue Get<TValue>(this Dictionary<string, TValue> dictionary, string key, bool ignoreCase)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (ignoreCase)
            {
                var thekey = dictionary.Keys.FirstOrDefault(k => k.ToLower() == key.ToLower());
                return dictionary[thekey];
            }
            else
            {
                return dictionary[key];
            }
        }

        public static void Set<TValue>(this Dictionary<string, TValue> dictionary, string key, TValue value, bool ignoreCase)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (ignoreCase)
            {
                var thekey = dictionary.Keys.FirstOrDefault(k => k.ToLower() == key.ToLower());
                dictionary[thekey] = value;
            }
            else
            {
                dictionary[key] = value;
            }
        }

        public static void Set(this Dictionary<string, string> dictionary, string key, string value, bool ignoreCase)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (ignoreCase)
            {
                var thekey = dictionary.Keys.FirstOrDefault(k => k.ToLower() == key.ToLower());
                dictionary[thekey] = value;
            }
            else
            {
                dictionary[key] = value;
            }
        }

        public static bool ContainsKey<TValue>(this Dictionary<string, TValue> dictionary, string key, bool ignoreCase)
        {
            if (dictionary == null) { return false; }
            if (string.IsNullOrEmpty(key)) { return false; }

            if (ignoreCase)
            {
                var result = dictionary.Keys.Any(k => k.ToLower() == key.ToLower());
                return result;
            }
            else
            {
                return dictionary.ContainsKey(key);
            }
        }

        public static Dictionary<string, string> Merge(this Dictionary<string, string> source, Dictionary<string, string> target)
        {
            if (target == null) return source;

            foreach (var item in target)
            {
                if (source.ContainsKey(item.Key))
                {
                    source[item.Key] = item.Value;
                }
                else
                {
                    source.Add(item.Key, item.Value);
                }
            }

            return source;
        }

        public static string ToSimpleTimeString(this TimeSpan span)
        {
            return span.ToString(@"hh\:mm\:ss");
        }

        public static string SafeTrim(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Trim();
        }

        public static bool HasValue(this string value)
        {
            return !string.IsNullOrEmpty(value);
        }
    }
}