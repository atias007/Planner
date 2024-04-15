﻿using Common;
using Microsoft.Extensions.Configuration;

namespace RabbitMQCheck;

internal class Node(IConfigurationSection section) : BaseDefault(section), ICheckElemnt
{
    public bool? MemoryAlarm { get; private set; } = section.GetValue<bool?>("memory alarm");
    public bool? DiskFreeAlarm { get; private set; } = section.GetValue<bool?>("disk free alarm");

    public string Key => "nodes";
}