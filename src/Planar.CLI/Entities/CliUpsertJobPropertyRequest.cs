﻿using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliUpsertJobPropertyRequest : CliJobOrTriggerKey
    {
        [ActionProperty(DefaultOrder = 1)]
        [Required("key argument is required")]
        public string PropertyKey { get; set; } = string.Empty;

        [ActionProperty(DefaultOrder = 2)]
        [Required("value argument is required")]
        public string PropertyValue { get; set; } = string.Empty;
    }
}