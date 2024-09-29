﻿using Common;
using Microsoft.Extensions.Configuration;

namespace SqlQueryCheck;

internal class CheckQuery(IConfigurationSection section, Defaults defaults) :
    BaseDefault(section, defaults), INamedCheckElement, IVetoEntity, IIntervalEntity
{
    public string Name { get; } = section.GetValue<string>("name") ?? string.Empty;

    public string ConnectionStringName { get; } = section.GetValue<string>("connection string name") ?? string.Empty;

    public string Query { get; } = section.GetValue<string>("query") ?? string.Empty;

    public string? Message { get; } = section.GetValue<string>("message");

    public TimeSpan Timeout { get; } = section.GetValue<TimeSpan?>("timeout") ?? TimeSpan.FromSeconds(30);

    public TimeSpan? Interval { get; } = section.GetValue<TimeSpan?>("interval");

    public string Key => Name;

    //// =================== //

    public string? ConnectionString { get; set; }

    public bool Veto { get; set; }

    public string? VetoReason { get; set; }
}