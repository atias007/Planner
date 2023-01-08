﻿using CommonJob;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.API.Helpers;
using Planar.Service.Data;
using Planar.Service.General;
using Planar.Service.Listeners.Base;
using Quartz;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using DbJobInstanceLog = Planar.Service.Model.JobInstanceLog;

namespace Planar.Service.Listeners
{
    public class LogJobListener : BaseListener<LogJobListener>, IJobListener
    {
        public LogJobListener(IServiceScopeFactory serviceScopeFactory, ILogger<LogJobListener> logger) : base(serviceScopeFactory, logger)
        {
        }

        public string Name => nameof(LogJobListener);

        public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            var result = Task.CompletedTask;
            try
            {
                if (IsSystemJob(context.JobDetail)) { return result; }
                ExecuteDal<HistoryData>(d => d.SetJobInstanceLogStatus(context.FireInstanceId, StatusMembers.Veto)).Wait(cancellationToken);
            }
            catch (Exception ex)
            {
                LogCritical(nameof(JobExecutionVetoed), ex);
            }
            finally
            {
                result = SafeScan(MonitorEvents.ExecutionVetoed, context, null);
            }

            return result;
        }

        public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            var result = Task.CompletedTask;

            try
            {
                if (IsSystemJob(context.JobDetail)) { return result; }

                string data = GetJobDataForLogging(context.MergedJobDataMap);

                var log = new DbJobInstanceLog
                {
                    InstanceId = context.FireInstanceId,
                    Data = data,
                    StartDate = context.FireTimeUtc.ToLocalTime().DateTime,
                    Status = (int)StatusMembers.Running,
                    StatusTitle = StatusMembers.Running.ToString(),
                    JobId = JobKeyHelper.GetJobId(context.JobDetail),
                    JobName = context.JobDetail.Key.Name,
                    JobGroup = context.JobDetail.Key.Group,
                    TriggerId = TriggerKeyHelper.GetTriggerId(context.Trigger),
                    TriggerName = context.Trigger.Key.Name,
                    TriggerGroup = context.Trigger.Key.Group,
                    Retry = context.Trigger.Key.Group == Consts.RetryTriggerGroup,
                    ServerName = Environment.MachineName
                };

                log.TriggerId ??= Consts.ManualTriggerId;
                if (log.Data?.Length > 4000) { log.Data = log.Data[0..4000]; }
                if (log.JobId?.Length > 20) { log.JobId = log.JobId[0..20]; }
                if (log.JobName.Length > 50) { log.JobName = log.JobName[0..50]; }
                if (log.JobGroup.Length > 50) { log.JobGroup = log.JobGroup[0..50]; }
                if (log.TriggerId.Length > 20) { log.TriggerId = log.TriggerId[0..20]; }
                if (log.TriggerName.Length > 50) { log.TriggerName = log.TriggerName[0..50]; }
                if (log.TriggerGroup.Length > 50) { log.TriggerGroup = log.TriggerGroup[0..50]; }
                if (log.InstanceId.Length > 250) { log.InstanceId = log.InstanceId[0..250]; }
                if (log.ServerName.Length > 50) { log.ServerName = log.ServerName[0..50]; }

                ExecuteDal<HistoryData>(d => d.CreateJobInstanceLog(log)).Wait(cancellationToken);
            }
            catch (Exception ex)
            {
                LogCritical(nameof(JobToBeExecuted), ex);
            }
            finally
            {
                result = SafeScan(MonitorEvents.ExecutionStart, context, null);
            }

            return result;
        }

        public Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken = default)
        {
            var result = Task.CompletedTask;
            Exception executionException = null;

            try
            {
                if (IsSystemJob(context.JobDetail)) { return result; }

                var unhadleException = JobExecutionMetadata.GetInstance(context)?.UnhandleException;
                executionException = unhadleException ?? jobException;

                var duration = context.JobRunTime.TotalMilliseconds;
                var endDate = context.FireTimeUtc.ToLocalTime().DateTime.Add(context.JobRunTime);
                var status = executionException == null ? StatusMembers.Success : StatusMembers.Fail;

                var metadata = context.Result as JobExecutionMetadata;

                var log = new DbJobInstanceLog
                {
                    InstanceId = context.FireInstanceId,
                    Duration = Convert.ToInt32(duration),
                    EndDate = endDate,
                    Exception = executionException?.ToString(),
                    EffectedRows = metadata?.EffectedRows,
                    Log = metadata?.Log.ToString(),
                    Status = (int)status,
                    StatusTitle = status.ToString(),
                    IsStopped = context.CancellationToken.IsCancellationRequested
                };

                ExecuteDal<HistoryData>(d => d.UpdateHistoryJobRunLog(log)).Wait(cancellationToken);
            }
            catch (Exception ex)
            {
                LogCritical(nameof(JobWasExecuted), ex);
            }
            finally
            {
                result = SafeMonitorJobWasExecuted(context, executionException);
            }

            return result;
        }

        private async Task SafeMonitorJobWasExecuted(IJobExecutionContext context, Exception exception)
        {
            var allTasks = new List<Task>();
            await SafeScan(MonitorEvents.ExecutionEnd, context, exception);

            var success = exception == null;
            if (success)
            {
                var task1 = SafeScan(MonitorEvents.ExecutionSuccess, context, exception);
                allTasks.Add(task1);

                // Execution sucsses with no effected rows
                var effectedRows = ServiceUtil.GetEffectedRows(context);
                if (effectedRows == 0)
                {
                    var task2 = SafeScan(MonitorEvents.ExecutionSuccessWithNoEffectedRows, context, exception);
                    allTasks.Add(task2);
                }
            }
            else
            {
                var task3 = SafeScan(MonitorEvents.ExecutionFail, context, exception);
                var task4 = SafeScan(MonitorEvents.ExecutionFailxTimesInRow, context, exception);
                var task5 = SafeScan(MonitorEvents.ExecutionFailxTimesInHour, context, exception);
                var task6 = SafeScan(MonitorEvents.ExecutionFailWithEffectedRowsGreaterThanx, context, exception);
                var task7 = SafeScan(MonitorEvents.ExecutionFailWithEffectedRowsLessThanx, context, exception);

                allTasks.Add(task3);
                allTasks.Add(task4);
                allTasks.Add(task5);
                allTasks.Add(task6);
                allTasks.Add(task7);
            }

            await Task.WhenAll(allTasks);
        }

        private static string GetJobDataForLogging(JobDataMap data)
        {
            if (data?.Count == 0) { return null; }

            var items = Global.ConvertDataMapToDictionary(data);
            if (items?.Count == 0) { return null; }

            var yml = new SerializerBuilder().Build().Serialize(items);
            return yml;
        }
    }
}