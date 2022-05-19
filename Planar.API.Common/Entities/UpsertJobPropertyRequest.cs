﻿using Planar.API.Common.Validation;

namespace Planar.API.Common.Entities
{
    public class UpsertJobPropertyRequest : JobOrTriggerKey
    {
        [Required]
        public string PropertyKey { get; set; }

        [Required]
        public string PropertyValue { get; set; }
    }
}