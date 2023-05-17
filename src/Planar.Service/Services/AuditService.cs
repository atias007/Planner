﻿using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Common.Helpers;
using Planar.Service.API.Helpers;
using Planar.Service.Audit;
using Planar.Service.Data;
using Planar.Service.Model;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Planar.Service.Services
{
    public class AuditService : BackgroundService
    {
        private readonly Channel<AuditMessage> _channel;
        private readonly ILogger<AuditService> _logger;
        private readonly JobKeyHelper _jobKeyHelper;
        private readonly IScheduler _scheduler;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public AuditService(IServiceProvider serviceProvider, IServiceScopeFactory serviceScopeFactory)
        {
            _channel = serviceProvider.GetRequiredService<Channel<AuditMessage>>();
            _logger = serviceProvider.GetRequiredService<ILogger<AuditService>>();
            _jobKeyHelper = serviceProvider.GetRequiredService<JobKeyHelper>();
            _scheduler = serviceProvider.GetRequiredService<IScheduler>();
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var reader = _channel.Reader;
            while (!reader.Completion.IsCompleted && await reader.WaitToReadAsync(stoppingToken))
            {
                if (reader.TryRead(out var msg))
                {
                    await SafeSaveAudit(msg);
                }
            }

            _channel.Writer.TryComplete();
        }

        private async Task SafeSaveAudit(AuditMessage message)
        {
            try
            {
                await SaveAudit(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "fail to save job audit item. message: {@Message}", message);
            }
        }

        private async Task SaveAudit(AuditMessage message)
        {
            var usernameClaim = message.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var surnameClaim = message.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value;
            var givenNameClaim = message.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value;
            var title = $"{givenNameClaim} {surnameClaim}".Trim();

            string? triggerId;
            ITrigger? trigger;

            if (message.TriggerKey != null)
            {
                trigger = await _scheduler.GetTrigger(message.TriggerKey);
                triggerId = TriggerHelper.GetTriggerId(trigger);
                message.Description = message.Description.Replace("{{TriggerId}}", $"trigger id: {triggerId}");

                if (message.JobKey == null && trigger != null)
                {
                    message.JobKey = trigger.JobKey;
                }

                if (message.AddTriggerInfo)
                {
                    var info = new List<object>();
                    if (message.AdditionalInfo != null) { info.Add(message.AdditionalInfo); }

                    var details = GetTriggerDetails(trigger);
                    if (details != null) { info.Add(new { trigger = details }); }

                    message.AdditionalInfo = info;
                }
            }

            var jobId = message.JobKey == null ? string.Empty : await _jobKeyHelper.GetJobId(message.JobKey);

            using var scope = _serviceScopeFactory.CreateScope();
            var data = scope.ServiceProvider.GetRequiredService<JobData>();
            var audit = new JobAudit
            {
                DateCreated = DateTime.Now,
                Description = message.Description,
                AdditionalInfo = message.AdditionalInfo == null ? null : YmlUtil.Serialize(message.AdditionalInfo),
                JobId = jobId ?? string.Empty,
                Username = usernameClaim ?? Roles.Anonymous.ToString().ToLower(),
                UserTitle = title ?? Roles.Anonymous.ToString().ToLower(),
                JobKey = message.JobKey == null ? string.Empty : $"{message.JobKey.Group}.{message.JobKey.Name}"
            };

            await data.AddJobAudit(audit);
        }

        private TriggerDetails? GetTriggerDetails(ITrigger? trigger)
        {
            if (trigger == null) { return null; }

            using var scope = _serviceScopeFactory.CreateScope();
            var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

            if (trigger is ISimpleTrigger t1)
            {
                return mapper.Map<SimpleTriggerDetails>(t1);
            }

            if (trigger is ICronTrigger t2)
            {
                return mapper.Map<CronTriggerDetails>(t2);
            }

            return null;
        }
    }
}