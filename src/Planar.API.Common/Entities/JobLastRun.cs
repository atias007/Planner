﻿using System;

namespace Planar.API.Common.Entities
{
    public class JobLastRun
    {
        public long Id { get; set; }

        public string JobId { get; set; } = null!;

        public string JobName { get; set; } = null!;

        public string JobGroup { get; set; } = null!;

        public string JobType { get; set; } = null!;

        public string TriggerId { get; set; } = null!;

        public int Status { get; set; }

        public string? StatusTitle { get; set; }

        public DateTime StartDate { get; set; }

        public int? Duration { get; set; }

        public int? EffectedRows { get; set; }
    }
}