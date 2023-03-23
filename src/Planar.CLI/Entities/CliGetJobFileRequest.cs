﻿using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetJobFileRequest
    {
        [ActionProperty(DefaultOrder = 0)]
        [Required("name argument is required")]
        public string Name { get; set; } = string.Empty;

        [ActionProperty("o", "output")]
        public string? OutputFilename { get; set; }
    }
}