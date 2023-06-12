﻿namespace Planar.API.Common.Entities
{
    public enum AllJobsMembers
    {
        AllUserJobs,
        AllSystemJobs,
        All
    }

    public class GetAllJobsRequest
    {
        public AllJobsMembers Filter { get; set; }

        public string? JobType { get; set; }

        public string? Group { get; set; }

        public bool? Active { get; set; }
    }
}