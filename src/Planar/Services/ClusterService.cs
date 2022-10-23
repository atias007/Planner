using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.General;
using Quartz;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Planar
{
    internal class ClusterService : PlanarCluster.PlanarClusterBase
    {
        private readonly ILogger<ClusterService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ClusterService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = _serviceProvider.GetRequiredService<ILogger<ClusterService>>();
        }

        // OK
        public override async Task<Empty> HealthCheck(Empty request, ServerCallContext context)
        {
            SchedulerUtil.HealthCheck(_logger);
            return await Task.FromResult(new Empty());
        }

        // OK
        public override async Task<Empty> StopScheduler(Empty request, ServerCallContext context)
        {
            await SchedulerUtil.Stop(context.CancellationToken);
            return new Empty();
        }

        // OK
        public override async Task<Empty> StartScheduler(Empty request, ServerCallContext context)
        {
            await SchedulerUtil.Start(context.CancellationToken);
            return new Empty();
        }

        // OK
        public override async Task<IsJobRunningReply> IsJobRunning(RpcJobKey request, ServerCallContext context)
        {
            ValidateRequest(request);

            var jobKey = new JobKey(request.Name, request.Group);
            var result = await SchedulerUtil.IsJobRunning(jobKey, context.CancellationToken);
            return new IsJobRunningReply { IsRunning = result };
        }

        // OK
        public override async Task<RunningJobReply> GetRunningJob(GetRunningJobRequest request, ServerCallContext context)
        {
            ValidateRequest(request);
            var job = await SchedulerUtil.GetRunningJob(request.InstanceId, context.CancellationToken);
            var item = MapRunningJobReply(job);
            return item ?? new RunningJobReply { IsEmpty = true };
        }

        // OK
        public override async Task<GetRunningJobsReply> GetRunningJobs(Empty request, ServerCallContext context)
        {
            var result = new GetRunningJobsReply();
            var jobs = await SchedulerUtil.GetRunningJobs(context.CancellationToken);

            foreach (var j in jobs)
            {
                var item = MapRunningJobReply(j);
                if (item != null)
                {
                    result.Jobs.Add(item);
                }
            }

            return result;
        }

        // OK
        public override async Task<RunningDataReply> GetRunningData(GetRunningJobRequest request, ServerCallContext context)
        {
            ValidateRequest(request);
            var job = await SchedulerUtil.GetRunningData(request.InstanceId, context.CancellationToken);
            if (job == null)
            {
                return new RunningDataReply { IsEmpty = true };
            }

            var result = new RunningDataReply
            {
                Exceptions = SafeString(job.Exceptions),
                Log = SafeString(job.Log)
            };

            return result;
        }

        // OK
        public override async Task<IsRunningInstanceExistReply> IsRunningInstanceExist(GetRunningJobRequest request, ServerCallContext context)
        {
            ValidateRequest(request);
            var result = await SchedulerUtil.IsRunningInstanceExistOnLocal(request.InstanceId, context.CancellationToken);
            return new IsRunningInstanceExistReply { Exists = result };
        }

        // OK
        public override async Task<StopRunningJobReply> StopRunningJob(GetRunningJobRequest request, ServerCallContext context)
        {
            ValidateRequest(request);
            var util = _serviceProvider.GetRequiredService<ClusterUtil>();
            var result = await SchedulerUtil.StopRunningJob(request.InstanceId, util, context.CancellationToken);
            return new StopRunningJobReply { IsStopped = result };
        }

        // OK
        public override async Task<PersistanceRunningJobInfoReply> GetPersistanceRunningJobInfo(Empty request, ServerCallContext context)
        {
            var result = new PersistanceRunningJobInfoReply();
            var jobs = await SchedulerUtil.GetPersistanceRunningJobsInfo();
            var items = jobs.Select(j => new PersistanceRunningJobInfo
            {
                Exceptions = SafeString(j.Exceptions),
                Group = SafeString(j.Group),
                Log = SafeString(j.Log),
                InstanceId = SafeString(j.InstanceId),
                Name = SafeString(j.Name),
                Duration = j.Duration
            });

            result.RunningJobs.AddRange(items);
            return result;
        }

        public override async Task<IsJobAssestsExistReply> IsJobFolderExist(IsJobAssestsExistRequest request, ServerCallContext context)
        {
            var result = new IsJobAssestsExistReply
            {
                Exists = ServiceUtil.IsJobFolderExists(request.Folder),
                Path = ServiceUtil.GetJobFolder(request.Folder)
            };

            return await Task.FromResult(result);
        }

        public override async Task<IsJobAssestsExistReply> IsJobFileExist(IsJobAssestsExistRequest request, ServerCallContext context)
        {
            var result = new IsJobAssestsExistReply
            {
                Exists = ServiceUtil.IsJobFileExists(request.Folder, request.Filename),
                Path = ServiceUtil.GetJobFolder(request.Folder)
            };

            return await Task.FromResult(result);
        }

        private static void ValidateRequest(object request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
        }

        private static string SafeString(string value)
        {
            if (value == null) { return string.Empty; }
            return value;
        }

        private static RunningJobReply MapRunningJobReply(RunningJobDetails job)
        {
            if (job == null) { return null; }

            var item = new RunningJobReply
            {
                Description = SafeString(job.Description),
                EffectedRows = job.EffectedRows == null ? -1 : job.EffectedRows.Value,
                FireInstanceId = SafeString(job.FireInstanceId),
                Group = SafeString(job.Group),
                Id = job.Id,
                Name = SafeString(job.Name),
                Progress = job.Progress,
                RunTime = Duration.FromTimeSpan(job.RunTime),
                TriggerGroup = SafeString(job.TriggerGroup),
                TriggerId = SafeString(job.TriggerId),
                TriggerName = SafeString(job.TriggerName),
                RefireCount = job.RefireCount,
                FireTime = GetTimeStamp(job.FireTime),
                NextFireTime = GetTimeStamp(job.NextFireTime),
                PreviousFireTime = GetTimeStamp(job.PreviousFireTime),
                ScheduledFireTime = GetTimeStamp(job.ScheduledFireTime),
            };

            foreach (var d in job.DataMap)
            {
                item.DataMap.Add(new DataMap { Key = d.Key, Value = d.Value });
            }

            return item;
        }

        private static Timestamp GetTimeStamp(DateTime date)
        {
            if (date == default) { return default; }
            var offset = new DateTimeOffset(date);
            var timestamp = Timestamp.FromDateTimeOffset(offset);
            return timestamp;
        }

        private static Timestamp GetTimeStamp(DateTime? date)
        {
            if (date == null) { return default; }
            if (date.Value == DateTime.MinValue) { return default; }
            var offset = new DateTimeOffset(date.Value);
            var timestamp = Timestamp.FromDateTimeOffset(offset);
            return timestamp;
        }
    }
}