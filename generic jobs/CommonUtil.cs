﻿namespace Common;

internal static class CommonUtil
{
    public static long? GetSize(string? value, string fieldName)
    {
        var factor = 0;
        if (string.IsNullOrWhiteSpace(value)) { return null; }

        value = value.Trim().ToLower();
        var cleanValue = string.Empty;
        if (value.EndsWith("bytes"))
        {
            factor = 0;
            cleanValue = value.Replace("bytes", string.Empty);
        }
        else if (value.EndsWith("kb"))
        {
            factor = 1;
            cleanValue = value.Replace("kb", string.Empty);
        }
        else if (value.EndsWith("mb"))
        {
            factor = 2;
            cleanValue = value.Replace("mb", string.Empty);
        }
        else if (value.EndsWith("gb"))
        {
            factor = 3;
            cleanValue = value.Replace("gb", string.Empty);
        }
        else if (value.EndsWith("tb"))
        {
            factor = 4;
            cleanValue = value.Replace("tb", string.Empty);
        }
        else if (value.EndsWith("pb"))
        {
            factor = 5;
            cleanValue = value.Replace("pb", string.Empty);
        }

        if (!int.TryParse(cleanValue, out var number))
        {
            throw new InvalidDataException($"'{fieldName}' has invalid value. value should be numeric with optional one of the following suffix: bytes, kb, mb, gb, tb, pb (e.g. 10kb, 2.5mb, 10.33gb, etc.)");
        }

        return number * (int)Math.Pow(1024, factor);
    }

    public static DateTime? GetDateFromSpan(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value)) { return null; }

        value = value.Trim().ToLower();
        long cleanValue;
        if (value.EndsWith("seconds"))
        {
            cleanValue = GetCleanValue(value, "seconds");
            return DateTime.Now.AddSeconds(-cleanValue);
        }
        else if (value.EndsWith("minutes"))
        {
            cleanValue = GetCleanValue(value, "minutes");
            return DateTime.Now.AddMinutes(-cleanValue);
        }
        else if (value.EndsWith("hours"))
        {
            cleanValue = GetCleanValue(value, "hours");
            return DateTime.Now.AddHours(-cleanValue);
        }
        else if (value.EndsWith("days"))
        {
            cleanValue = GetCleanValue(value, "days");
            return DateTime.Now.AddDays(-cleanValue);
        }
        else if (value.EndsWith("weeks"))
        {
            cleanValue = GetCleanValue(value, "weeks");
            return DateTime.Now.AddDays(-cleanValue * 7);
        }
        else if (value.EndsWith("months"))
        {
            var intCleanValue = Convert.ToInt32(GetCleanValue(value, "months"));
            return DateTime.Now.AddMonths(-intCleanValue);
        }
        else if (value.EndsWith("years"))
        {
            var intCleanValue = Convert.ToInt32(GetCleanValue(value, "years"));
            return DateTime.Now.AddYears(-intCleanValue);
        }
        else
        {
            throw GetException();
        }

        long GetCleanValue(string value, string replace)
        {
            var cleanValue = value.Replace(replace, string.Empty);
            if (!long.TryParse(cleanValue, out var longValue))
            {
                throw GetException();
            }

            return longValue;
        }

        Exception GetException()
        {
            return new InvalidDataException($"'{fieldName}' has invalid value. value should be in the format of 'hh:mm:ss' (e.g. 1:30:00, 0:30:00, etc.)");
        }
    }

    public static void ValidateItems(IEnumerable<ICheckElemnt> item, string sectionName, string keyName)
    {
        if (item == null || !item.Any())
        {
            throw new InvalidDataException($"'{sectionName}' section is null or empty");
        }

        var duplicates1 = item.GroupBy(x => x.Key).Where(g => g.Count() > 1).Select(y => y.Key).ToList();
        if (duplicates1.Count != 0)
        {
            throw new InvalidDataException($"duplicated fount at '{sectionName}' section. duplicate '{keyName}' found: {string.Join(", ", duplicates1)}");
        }
    }
}