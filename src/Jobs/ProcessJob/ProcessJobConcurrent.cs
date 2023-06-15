﻿using Microsoft.Extensions.Logging;
using Planar.Common;

namespace Planar
{
    public class ProcessJobConcurrent : ProcessJob
    {
        public ProcessJobConcurrent(ILogger<ProcessJob> logger, IJobPropertyDataLayer dataLayer) : base(logger, dataLayer)
        {
        }
    }
}