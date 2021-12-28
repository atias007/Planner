﻿using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Planner.MonitorHook
{
    public interface IMonitorHook
    {
        Task Handle(IMonitorDetails monitorDetails, ILogger logger);
    }
}