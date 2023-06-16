﻿using Microsoft.Extensions.Logging;
using Planar;
using Planar.Common;
using Planar.Common.Helpers;
using Planar.Job;
using Planar.Service.API.Helpers;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using IJobExecutionContext = Quartz.IJobExecutionContext;

namespace CommonJob
{
    public abstract class BaseCommonJob
    {
        protected static readonly string? IgnoreDataMapAttribute = typeof(IgnoreDataMapAttribute).FullName;
        protected static readonly string? JobDataMapAttribute = typeof(JobDataAttribute).FullName;
        protected static readonly string? TriggerDataMapAttribute = typeof(TriggerDataAttribute).FullName;
        private JobMessageBroker _messageBroker = null!;

        protected IDictionary<string, string?> Settings { get; private set; } = new Dictionary<string, string?>();

        protected JobMessageBroker MessageBroker => _messageBroker;

        protected static void DoNothingMethod()
        {
            //// *** Do Nothing Method *** ////
        }

        protected static void HandleException(IJobExecutionContext context, Exception ex)
        {
            var metadata = JobExecutionMetadata.GetInstance(context);
            if (ex is TargetInvocationException)
            {
                metadata.UnhandleException = ex.InnerException;
            }
            else
            {
                metadata.UnhandleException = ex;
            }
        }

        protected async Task WaitForJobTask(IJobExecutionContext context, Task task)
        {
            var timeout = TriggerHelper.GetTimeoutWithDefault(context.Trigger);
            var finish = task.Wait(timeout);
            if (!finish)
            {
                MessageBroker.AppendLog(LogLevel.Warning, $"Timeout occur, sent cancel requst to job (timeout value: {FormatTimeSpan(timeout)})");
                await context.Scheduler.Interrupt(context.JobDetail.Key);
            }

            task.Wait();
        }

        internal void FillSettings(IDictionary<string, string?> settings)
        {
            Settings = settings;
        }

        internal void SetMessageBroker(JobMessageBroker messageBroker)
        {
            _messageBroker = messageBroker;
        }

        private static string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalSeconds < 1) { return $"{timeSpan.TotalMilliseconds:N0}ms"; }
            if (timeSpan.TotalDays >= 1) { return $"{timeSpan:\\(d\\)\\ hh\\:mm\\:ss}"; }
            return $"{timeSpan:hh\\:mm\\:ss}";
        }
    }

    public abstract class BaseCommonJob<TInstance, TProperties> : BaseCommonJob, IJob
    where TInstance : class
    where TProperties : class, new()
    {
        protected readonly ILogger<TInstance> _logger;
        private readonly IJobPropertyDataLayer _dataLayer;

        protected BaseCommonJob(ILogger<TInstance> logger, IJobPropertyDataLayer dataLayer)
        {
            _logger = logger;
            _dataLayer = dataLayer;
        }

        public TProperties Properties { get; private set; } = new();

        public abstract Task Execute(IJobExecutionContext context);

        protected void FinalizeJob(IJobExecutionContext context)
        {
            try
            {
                var metadata = JobExecutionMetadata.GetInstance(context);
                metadata.Progress = 100;
            }
            catch (Exception ex)
            {
                var source = nameof(FinalizeJob);
                _logger.LogError(ex, "Fail at {Source} with job {Group}.{Name}", source, context.JobDetail.Key.Group, context.JobDetail.Key.Name);
                throw;
            }
        }

        protected async Task Initialize(IJobExecutionContext context, IMonitorUtil? monitorUtil = null)
        {
            await SetProperties(context);

            string? path = null;
            if (Properties is IPathJobProperties pathProperties)
            {
                path = pathProperties.Path;
            }

            FillSettings(LoadJobSettings(path));
            SetMessageBroker(new JobMessageBroker(context, Settings, monitorUtil));

            context.CancellationToken.Register(() =>
            {
                MessageBroker.AppendLog(LogLevel.Warning, "Service get a request for cancel job");
            });
        }

        protected IDictionary<string, string?> LoadJobSettings(string? path)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return new Dictionary<string, string?>();
                var jobSettings = JobSettingsLoader.LoadJobSettings(path);
                return jobSettings;
            }
            catch (Exception ex)
            {
                var source = nameof(LoadJobSettings);
                _logger.LogError(ex, "Fail at {Source}", source);
                throw;
            }
        }

        protected void MapJobInstanceProperties(IJobExecutionContext context, Type targetType, object instance)
        {
            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on Planar.Job.Test *****

            try
            {
                var allProperties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
                foreach (var item in context.MergedJobDataMap)
                {
                    if (item.Key.StartsWith(Consts.ConstPrefix)) { continue; }
                    var prop = allProperties.Find(p => string.Equals(p.Name, item.Key, StringComparison.OrdinalIgnoreCase));
                    MapProperty(context.JobDetail.Key, instance, prop, item);
                }
            }
            catch (Exception ex)
            {
                var source = nameof(MapJobInstanceProperties);
                _logger.LogError(ex, "Fail at {Source} with job {Group}.{Name}", source, context.JobDetail.Key.Group, context.JobDetail.Key.Name);
                throw;
            }

            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on Planar.Job.Test *****
        }

        protected void MapJobInstancePropertiesBack(IJobExecutionContext context, Type? targetType, object? instance)
        {
            //// ***** Attention: be aware for sync code with MapJobInstancePropertiesBack on Planar.Job.Test *****

            try
            {
                if (context == null) { return; }
                if (targetType == null) { return; }
                if (instance == null) { return; }

                var propInfo = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
                foreach (var prop in propInfo)
                {
                    if (prop.Name.StartsWith(Consts.ConstPrefix)) { continue; }
                    SafePutData(context, instance, prop);
                }
            }
            catch (Exception ex)
            {
                var source = nameof(MapJobInstancePropertiesBack);
                _logger.LogError(ex, "Fail at {Source} with job {Group}.{Name}", source, context.JobDetail.Key.Group, context.JobDetail.Key.Name);
                throw;
            }

            //// ***** Attention: be aware for sync code with MapJobInstancePropertiesBack on Planar.Job.Test *****
        }

        protected void ValidateMandatoryString(string? value, string propertyName)
        {
            if (!string.IsNullOrEmpty(value)) { value = value.Trim(); }
            if (string.IsNullOrEmpty(value))
            {
                throw new PlanarJobException($"property {propertyName} is mandatory for job '{GetType().FullName}'");
            }
        }

        private bool IsIgnoreProperty(PropertyInfo property, JobKey jobKey, KeyValuePair<string, object> data)
        {
            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on Planar.Job.Test *****

            var attributes = property.GetCustomAttributes();
            var ignore = attributes.Any(a => a.GetType().FullName == IgnoreDataMapAttribute);

            if (ignore)
            {
                _logger.LogDebug("Ignore map data key '{DataKey}' with value '{DataValue}' to property {PropertyName} of job '{JobGroup}.{JobName}'",
                    data.Key,
                    data.Value,
                    property.Name,
                    jobKey.Group,
                    jobKey.Name);
            }

            return ignore;

            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on Planar.Job.Test *****
        }

        private bool IsIgnoreProperty(IEnumerable<Attribute> attributes, PropertyInfo property, JobKey jobKey)
        {
            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on Planar.Job.Test *****

            var ignore = attributes.Any(a => a.GetType().FullName == IgnoreDataMapAttribute);

            if (ignore)
            {
                _logger.LogDebug("ATTENTION: Ignore map back property {PropertyName} of job '{JobGroup}.{JobName}' to data map",
                    property.Name,
                    jobKey.Group,
                    jobKey.Name);
            }

            return ignore;

            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on Planar.Job.Test *****
        }

        private void MapProperty(JobKey jobKey, object instance, PropertyInfo? prop, KeyValuePair<string, object> data)
        {
            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on Planar.Job.Test *****

            if (prop == null) { return; }

            try
            {
                var ignore = IsIgnoreProperty(prop, jobKey, data);
                if (ignore) { return; }

                var underlyingType = Nullable.GetUnderlyingType(prop.PropertyType);
                var finalType = underlyingType ?? prop.PropertyType;

                // nullable property with null value in data
                if (underlyingType != null && string.IsNullOrEmpty(PlanarConvert.ToString(data.Value))) { return; }

                var value = Convert.ChangeType(data.Value, finalType);
                prop.SetValue(instance, value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Fail to map data key '{Key}' with value {Value} to property {Name} of job {JobGroup}.{JobName}",
                    data.Key, data.Value, prop.Name, jobKey.Group, jobKey.Name);
            }

            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on Planar.Job.Test *****
        }

        private void SafePutData(IJobExecutionContext context, object instance, PropertyInfo prop)
        {
            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on Planar.Job.Test *****

            var attributes = prop.GetCustomAttributes();
            var ignore = IsIgnoreProperty(attributes, prop, context.JobDetail.Key);
            if (ignore) { return; }
            var jobData = attributes.Any(a => a.GetType().FullName == JobDataMapAttribute);
            var triggerData = attributes.Any(a => a.GetType().FullName == JobDataMapAttribute);

            if (jobData)
            {
                SafePutJobDataMap(context, instance, prop);
            }

            if (triggerData)
            {
                SafePutTiggerDataMap(context, instance, prop);
            }

            if (!jobData && !triggerData)
            {
                if (context.JobDetail.JobDataMap.ContainsKey(prop.Name))
                {
                    SafePutJobDataMap(context, instance, prop);
                }

                if (context.Trigger.JobDataMap.ContainsKey(prop.Name))
                {
                    SafePutTiggerDataMap(context, instance, prop);
                }
            }

            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on Planar.Job.Test *****
        }

        private void SafePutJobDataMap(IJobExecutionContext context, object instance, PropertyInfo prop)
        {
            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on Planar.Job.Test *****

            string? value = null;
            try
            {
                if (!Consts.IsDataKeyValid(prop.Name))
                {
                    throw new PlanarJobException($"the data key {prop.Name} in invalid");
                }

                value = PlanarConvert.ToString(prop.GetValue(instance));
                context.JobDetail.JobDataMap.Put(prop.Name, value);
            }
            catch (Exception ex)
            {
                var jobKey = context.JobDetail.Key;
                _logger.LogWarning(ex,
                    "Fail to save back value {Value} from property {Name} to JobDetails at job {JobGroup}.{JobName}",
                    value, prop.Name, jobKey.Group, jobKey.Name);
            }

            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on Planar.Job.Test *****
        }

        private void SafePutTiggerDataMap(IJobExecutionContext context, object instance, PropertyInfo prop)
        {
            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on Planar.Job.Test *****

            string? value = null;
            try
            {
                if (!Consts.IsDataKeyValid(prop.Name))
                {
                    throw new PlanarJobException($"the data key {prop.Name} in invalid");
                }

                value = PlanarConvert.ToString(prop.GetValue(instance));
                context.Trigger.JobDataMap.Put(prop.Name, value);
            }
            catch (Exception ex)
            {
                var jobKey = context.JobDetail.Key;
                _logger.LogWarning(ex,
                    "Fail to save back value {Value} from property {Name} to TriggerDetails at job {JobGroup}.{JobName}",
                    value, prop.Name, jobKey.Group, jobKey.Name);
            }

            //// ***** Attention: be aware for sync code with MapJobInstanceProperties on Planar.Job.Test *****
        }

        private async Task SetProperties(IJobExecutionContext context)
        {
            var jobId = JobHelper.GetJobId(context.JobDetail);
            if (jobId == null)
            {
                var title = JobHelper.GetKeyTitle(context.JobDetail);
                throw new PlanarJobException($"fail to get job id while execute job {title}");
            }

            var properties = await _dataLayer.GetJobProperty(jobId);
            if (string.IsNullOrEmpty(properties))
            {
                var title = JobHelper.GetKeyTitle(context.JobDetail);
                throw new PlanarJobException($"fail to get job properties while execute job {title} (id: {jobId})");
            }

            Properties = YmlUtil.Deserialize<TProperties>(properties);
        }
    }
}