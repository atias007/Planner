﻿using Planar.Common.Helpers;
using Planar.Service.Exceptions;
using Planar.Service.SystemJobs;
using Quartz;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.General;

internal static class AutoResumeJobUtil
{
    public static async Task QueueResumeJob(IScheduler scheduler, IJobDetail jobDetail, TimeSpan span)
    {
        if (span == TimeSpan.Zero) { return; }
        var jobKey = new JobKey(typeof(CircuitBreakerJob).Name, Consts.PlanarSystemGroup);
        var job = await scheduler.GetJobDetail(jobKey) ?? throw new JobNotFoundException(jobKey);
        var triggers = await scheduler.GetTriggersOfJob(jobDetail.Key);
        var triggersStates = triggers.Select(async t => new { t.Key, State = await scheduler.GetTriggerState(t.Key) });
        var activeTriggers = triggersStates.Where(t => TriggerHelper.IsActiveState(t.Result.State)).Select(t => t.Result.Key);
        if (!activeTriggers.Any()) { return; }
        var triggerGroup = activeTriggers.First().Group;
        var triggerNames = activeTriggers.Select(t => t.Name);

        var triggerKey = new TriggerKey($"Resume.{jobDetail.Key}", Consts.CircuitBreakerTriggerGroup);
        var triggerId = ServiceUtil.GenerateId();
        var key = jobDetail.Key;
        var dueDate = DateTime.Now.Add(span);
        var newTrigger = TriggerBuilder.Create()
             .WithIdentity(triggerKey)
             .UsingJobData(Consts.TriggerId, triggerId)
             .UsingJobData("JobKey.Name", key.Name)
             .UsingJobData("JobKey.Group", key.Group)
             .UsingJobData("Trigger.Group", triggerGroup)
             .UsingJobData("Trigger.Names", string.Join(',', triggerNames))
             .UsingJobData("Created", DateTime.Now.ToString())
             .StartAt(dueDate)
             .WithSimpleSchedule(b =>
             {
                 b.WithRepeatCount(0)
                 .WithMisfireHandlingInstructionFireNow();
             })
             .ForJob(job);

        // Schedule Job
        await scheduler.ScheduleJob(newTrigger.Build());
    }
}