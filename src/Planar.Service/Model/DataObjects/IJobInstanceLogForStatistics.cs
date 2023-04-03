﻿namespace Planar.Service.Model.DataObjects
{
    internal interface IJobInstanceLogForStatistics
    {
        int Id { get; }
        string JobId { get; }
        int Status { get; }
        int? Duration { get; }
        int? EffectedRows { get; }
        bool IsStopped { get; }
        byte? Anomaly { get; set; }
    }
}