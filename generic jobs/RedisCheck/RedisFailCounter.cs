﻿using Planar.Job;

namespace RedisStreamCheck;

internal class RedisFailCounter(IBaseJob baseJob)
{
    public int IncrementFailCount(RedisKeyCheck stream)
    {
        var key = GetKey(stream);
        int count = 1;
        if (baseJob.Context.MergedJobDataMap.TryGet<int>(key, out var value))
        {
            count = value.GetValueOrDefault() + 1;
        }

        baseJob.PutJobData(key, Convert.ToString(count));
        return count;
    }

    public void ResetFailCount(RedisKeyCheck stream)
    {
        var key = GetKey(stream);
        baseJob.RemoveJobData(key);
    }

    private static string GetKey(RedisKeyCheck stream)
    {
        return $"fail.count_{stream.Key}";
    }
}