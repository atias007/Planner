﻿using System;

namespace Planar.CLI.Entities
{
    public class CliClusterNode
    {
        public string Server { get; set; }
        public short Port { get; set; }
        public string InstanceId { get; set; }
        public short ClusterPort { get; set; }
        public DateTime JoinDate { get; set; }
        public DateTime? HealthCheckDate { get; set; }
        public TimeSpan? HealthCheckGap { get; set; }
        public TimeSpan? HealthCheckGapDeviation { get; set; }
    }
}