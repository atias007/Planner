﻿using System;
using System.Globalization;
using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities;

public class JobCircuitBreakerMetadata
{
    private const string NullValue = "null";

    [YamlMember(Alias = "enabled")]
    public bool Enabled { get; set; }

    [YamlMember(Alias = "failure threshold")]
    public int FailureThreshold { get; set; }

    [YamlMember(Alias = "success threshold")]
    public int? SuccessThreshold { get; set; }

    [YamlMember(Alias = "pause span")]
    public TimeSpan? PauseSpan { get; set; }

    [YamlIgnore]
    public int FailCounter { get; set; }

    [YamlIgnore]
    public int SuccessCounter { get; set; }

    public override string ToString()
    {
        var pauseSpan = PauseSpan.HasValue ? PauseSpan.Value.ToString() : NullValue;
        return $"FC:{FailCounter},SC:{SuccessCounter},FT:{FailureThreshold},ST:{SuccessThreshold},PS:{pauseSpan}";
    }

    public void Reset()
    {
        FailCounter = 0;
        SuccessCounter = 0;
    }

    public static JobCircuitBreakerMetadata Parse(string value)
    {
        var parts = value.Split(',');
        if (parts.Length != 5) { throw new ArgumentException($"Invalid format: {value}"); }

        var result = new JobCircuitBreakerMetadata();
        foreach (var part in parts)
        {
            var keyValue = part.Split(':');
            if (keyValue.Length != 2) { throw new ArgumentException($"Invalid format: {value}"); }
            switch (keyValue[0].Trim())
            {
                case "FC":
                    if (!int.TryParse(keyValue[1], out var failCounter)) { throw new ArgumentException($"Invalid format: {value}. Invalid FailCounter"); }
                    result.FailCounter = failCounter;
                    break;

                case "SC":
                    if (!int.TryParse(keyValue[1], out var successCounter)) { throw new ArgumentException($"Invalid format: {value}. Invalid SuccessCounter"); }
                    result.SuccessCounter = successCounter;
                    break;

                case "FT":
                    if (!int.TryParse(keyValue[1], out var failureThreshold)) { throw new ArgumentException($"Invalid format: {value}. Invalid FailureThreshold"); }
                    result.FailureThreshold = failureThreshold;
                    break;

                case "ST":
                    if (!int.TryParse(keyValue[1], out var successThreshold)) { throw new ArgumentException($"Invalid format: {value}. Invalid SuccessThreshold"); }
                    result.SuccessThreshold = successThreshold;
                    break;

                case "PS":
                    if (keyValue[1] == NullValue) { break; }
                    if (!TimeSpan.TryParse(keyValue[1], CultureInfo.InvariantCulture, out var pauseSpan)) { throw new ArgumentException($"Invalid format: {value}. Invalid PauseSpan"); }
                    result.PauseSpan = pauseSpan;
                    break;

                default:
                    throw new ArgumentException($"Invalid format: {value}");
            }
        }

        return result;
    }
}