﻿using System;
using System.Collections.Generic;

namespace Planar.Job
{
    public class JobDetail : IJobDetail
    {
        public Key Key { get; set; }

        public string Description { get; set; }

        public Type JobType { get; set; }

        public Dictionary<string, string> JobDataMap { get; set; }

        public bool Durable { get; set; }

        public bool PersistJobDataAfterExecution { get; set; }

        public bool ConcurrentExecutionDisallowed { get; set; }

        public bool RequestsRecovery { get; set; }
    }
}