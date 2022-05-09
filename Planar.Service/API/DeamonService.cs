﻿using Planar.API.Common;
using Planar.API.Common.Entities;
using Planar.Common;
using Quartz;
using Serilog;
using System;
using System.Collections.Generic;

namespace Planar.Service.API
{
    public class DeamonService : BaseService, IPlanarCommand
    {
        public DeamonService()
            : base(MainService.Resolve<ILogger>())
        {
        }

        private static IScheduler Scheduler
        {
            get
            {
                return MainService.Scheduler;
            }
        }

        private static DeamonBL BL
        {
            get
            {
                return MainService.Resolve<DeamonBL>();
            }
        }

        public AddJobResponse AddJob(AddJobRequest request)
        {
            try
            {
                InitializeService(nameof(AddJob));
                ValidateEntity(request);
                var result = BL.AddJob(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<AddJobResponse>(ex, nameof(AddJob));
                return result;
            }
        }

        public GetServiceInfoResponse GetServiceInfo()
        {
            try
            {
                InitializeService(nameof(GetServiceInfo));
                var response = new GetServiceInfoResponse
                {
                    InStandbyMode = Scheduler.InStandbyMode,
                    IsShutdown = Scheduler.IsShutdown,
                    IsStarted = Scheduler.IsStarted,
                    SchedulerInstanceId = Scheduler.SchedulerInstanceId,
                    SchedulerName = Scheduler.SchedulerName,
                    Environment = Global.Environment,
                };

                return response;
            }
            catch (Exception ex)
            {
                var result = HandleException<GetServiceInfoResponse>(ex, nameof(GetServiceInfo));
                return result;
            }
        }

        public BaseResponse InvokeJob(InvokeJobRequest request)
        {
            try
            {
                InitializeService(nameof(InvokeJob));
                ValidateEntity(request);
                DeamonBL.InvokeJob(request).Wait();
                return BaseResponse.Empty;
            }
            catch (Exception ex)
            {
                var result = HandleException(ex, nameof(InvokeJob));
                return result;
            }
        }

        public BaseResponse PauseJob(JobOrTriggerKey request)
        {
            try
            {
                InitializeService(nameof(PauseJob));
                ValidateEntity(request);
                DeamonBL.PauseJob(request).Wait();
                return BaseResponse.Empty;
            }
            catch (Exception ex)
            {
                var result = HandleException(ex, nameof(PauseJob));
                return result;
            }
        }

        public BaseResponse ResumeJob(JobOrTriggerKey request)
        {
            try
            {
                InitializeService(nameof(ResumeJob));
                ValidateEntity(request);
                DeamonBL.ResumeJob(request).Wait();
                return BaseResponse.Empty;
            }
            catch (Exception ex)
            {
                var result = HandleException(ex, nameof(ResumeJob));
                return result;
            }
        }

        public BaseResponse StopScheduler(StopSchedulerRequest request)
        {
            try
            {
                InitializeService(nameof(StopScheduler));
                ValidateEntity(request);
                DeamonBL.StopScheduler(request).Wait();
                return BaseResponse.Empty;
            }
            catch (Exception ex)
            {
                var result = HandleException(ex, nameof(StopScheduler));
                return result;
            }
        }

        public BaseResponse<JobDetails> GetJobDetails(JobOrTriggerKey request)
        {
            try
            {
                InitializeService(nameof(GetJobDetails));
                ValidateEntity(request);
                var result = DeamonBL.GetJobDetails(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse<JobDetails>>(ex, nameof(GetJobDetails));
                return result;
            }
        }

        public BaseResponse<TriggerRowDetails> GetTriggersDetails(JobOrTriggerKey request)
        {
            try
            {
                InitializeService(nameof(GetJobDetails));
                ValidateEntity(request);
                var result = DeamonBL.GetTriggersDetails(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse<TriggerRowDetails>>(ex, nameof(GetJobDetails));
                return result;
            }
        }

        public BaseResponse<TriggerRowDetails> GetTriggerDetails(JobOrTriggerKey request)
        {
            try
            {
                InitializeService(nameof(GetTriggerDetails));
                var result = DeamonBL.GetTriggerDetails(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse<TriggerRowDetails>>(ex, nameof(GetTriggerDetails));
                return result;
            }
        }

        public GetRunningJobsResponse GetRunningJobs(FireInstanceIdRequest request)
        {
            try
            {
                InitializeService(nameof(GetRunningJobs));
                var result = DeamonBL.GetRunningJobs(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<GetRunningJobsResponse>(ex, nameof(GetRunningJobs));
                return result;
            }
        }

        public BaseResponse StopRunningJob(FireInstanceIdRequest request)
        {
            try
            {
                InitializeService(nameof(StopRunningJob));
                var result = DeamonBL.StopRunningJob(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse>(ex, nameof(StopRunningJob));
                return result;
            }
        }

        public BaseResponse UpsertJobData(JobDataRequest request)
        {
            try
            {
                InitializeService(nameof(UpsertJobData));
                ValidateEntity(request);
                var result = DeamonBL.UpsertJobData(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException(ex, nameof(UpsertJobData));
                return result;
            }
        }

        public BaseResponse RemoveJobData(RemoveJobDataRequest request)
        {
            try
            {
                InitializeService(nameof(RemoveJobData));
                ValidateEntity(request);
                var result = DeamonBL.RemoveJobData(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException(ex, nameof(RemoveJobData));
                return result;
            }
        }

        public BaseResponse ClearJobData(JobOrTriggerKey request)
        {
            try
            {
                InitializeService(nameof(ClearJobData));
                ValidateEntity(request);
                var result = DeamonBL.ClearJobData(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException(ex, nameof(ClearJobData));
                return result;
            }
        }

        public BaseResponse PauseAll()
        {
            try
            {
                InitializeService(nameof(PauseAll));
                var result = DeamonBL.PauseAll().Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException(ex, nameof(PauseAll));
                return result;
            }
        }

        public BaseResponse ResumeAll()
        {
            try
            {
                InitializeService(nameof(ResumeAll));
                var result = DeamonBL.ResumeAll().Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException(ex, nameof(ResumeAll));
                return result;
            }
        }

        public GetTraceResponse GetTrace(GetTraceRequest request)
        {
            try
            {
                InitializeService(nameof(GetTrace));
                var result = BL.GetTrace(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<GetTraceResponse>(ex, nameof(GetTrace));
                return result;
            }
        }

        public BaseResponse UpsertGlobalParameter(GlobalParameterData request)
        {
            try
            {
                InitializeService(nameof(UpsertGlobalParameter));
                var result = BL.UpsertGlobalParameter(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException(ex, nameof(UpsertGlobalParameter));
                return result;
            }
        }

        public BaseResponse RemoveGlobalParameter(GlobalParameterKey request)
        {
            try
            {
                InitializeService(nameof(RemoveGlobalParameter));
                var result = BL.RemoveGlobalParameter(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException(ex, nameof(RemoveGlobalParameter));
                return result;
            }
        }

        public BaseResponse<string> GetGlobalParameter(GlobalParameterKey request)
        {
            try
            {
                InitializeService(nameof(GetGlobalParameter));
                var result = BL.GetGlobalParameter(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse<string>>(ex, nameof(GetGlobalParameter));
                return result;
            }
        }

        public GetAllGlobalParametersResponse GetAllGlobalParameters()
        {
            try
            {
                InitializeService(nameof(GetAllGlobalParameters));
                var result = BL.GetAllGlobalParameters().Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<GetAllGlobalParametersResponse>(ex, nameof(GetAllGlobalParameters));
                return result;
            }
        }

        public BaseResponse FlushGlobalParameter()
        {
            try
            {
                InitializeService(nameof(FlushGlobalParameter));
                MainService.LoadGlobalParameters().Wait();
                return BaseResponse.Empty;
            }
            catch (Exception ex)
            {
                var result = HandleException(ex, nameof(FlushGlobalParameter));
                return result;
            }
        }

        public BaseResponse<Dictionary<string, string>> GetJobSettings(JobOrTriggerKey request)
        {
            try
            {
                InitializeService(nameof(GetJobSettings));
                var result = DeamonBL.GetJobSettings(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse<Dictionary<string, string>>>(ex, nameof(GetJobSettings));
                return result;
            }
        }

        public BaseResponse<List<string>> GetAllCalendars()
        {
            try
            {
                InitializeService(nameof(GetAllCalendars));
                var result = DeamonBL.GetAllCalendars().Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse<List<string>>>(ex, nameof(GetAllCalendars));
                return result;
            }
        }

        public BaseResponse RemoveTrigger(JobOrTriggerKey request)
        {
            try
            {
                InitializeService(nameof(RemoveTrigger));
                var result = DeamonBL.RemoveTrigger(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse>(ex, nameof(RemoveTrigger));
                return result;
            }
        }

        public BaseResponse PauseTrigger(JobOrTriggerKey request)
        {
            try
            {
                InitializeService(nameof(PauseTrigger));
                var result = DeamonBL.PauseTrigger(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse>(ex, nameof(PauseTrigger));
                return result;
            }
        }

        public BaseResponse ResumeTrigger(JobOrTriggerKey request)
        {
            try
            {
                InitializeService(nameof(ResumeTrigger));
                var result = DeamonBL.ResumeTrigger(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse>(ex, nameof(ResumeTrigger));
                return result;
            }
        }

        public BaseResponse AddTrigger(AddTriggerRequest request)
        {
            try
            {
                InitializeService(nameof(AddTrigger));
                var result = DeamonBL.AddTrigger(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse>(ex, nameof(AddTrigger));
                return result;
            }
        }

        public BaseResponse<LastInstanceId> GetLastInstanceId(GetLastInstanceIdRequest request)
        {
            try
            {
                InitializeService(nameof(GetLastInstanceId));
                var result = BL.GetLastInstanceId(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse<LastInstanceId>>(ex, nameof(GetLastInstanceId));
                return result;
            }
        }

        public BaseResponse<RunningJobDetails> GetRunningJob(FireInstanceIdRequest request)
        {
            try
            {
                InitializeService(nameof(GetRunningJob));
                var result = DeamonBL.GetRunningJob(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse<RunningJobDetails>>(ex, nameof(GetRunningJob));
                return result;
            }
        }

        public BaseResponse<GetTestStatusResponse> GetTestStatus(GetByIdRequest request)
        {
            try
            {
                InitializeService(nameof(GetTestStatus));
                var result = BL.GetTestStatus(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse<GetTestStatusResponse>>(ex, nameof(GetTestStatus));
                return result;
            }
        }

        public BaseResponse<string> GetTraceException(GetByIdRequest request)
        {
            try
            {
                InitializeService(nameof(GetTraceException));
                var result = BL.GetTraceException(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse<string>>(ex, nameof(GetTraceException));
                return result;
            }
        }

        public BaseResponse<string> GetTraceProperties(GetByIdRequest request)
        {
            try
            {
                InitializeService(nameof(GetTraceProperties));
                var result = BL.GetTraceProperties(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse<string>>(ex, nameof(GetTraceProperties));
                return result;
            }
        }

        public BaseResponse<AddUserResponse> AddUser(AddUserRequest request)
        {
            try
            {
                InitializeService(nameof(AddUser));
                ValidateEntity(request);
                var result = BL.AddUser(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse<AddUserResponse>>(ex, nameof(AddUser));
                return result;
            }
        }

        public BaseResponse<string> GetUser(GetByIdRequest request)
        {
            try
            {
                InitializeService(nameof(GetUser));
                ValidateEntity(request);
                var result = BL.GetUser(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse<string>>(ex, nameof(GetUser));
                return result;
            }
        }

        public BaseResponse<string> GetUsers()
        {
            try
            {
                InitializeService(nameof(GetUsers));
                var result = BL.GetUsers().Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse<string>>(ex, nameof(GetUsers));
                return result;
            }
        }

        public BaseResponse RemoveUser(GetByIdRequest request)
        {
            try
            {
                InitializeService(nameof(RemoveUser));
                ValidateEntity(request);
                var result = BL.RemoveUser(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse>(ex, nameof(RemoveUser));
                return result;
            }
        }

        public BaseResponse UpdateUser(string request)
        {
            try
            {
                InitializeService(nameof(UpdateUser));
                var result = BL.UpdateUser(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse>(ex, nameof(UpdateUser));
                return result;
            }
        }

        public BaseResponse<string> GetUserPassword(GetByIdRequest request)
        {
            try
            {
                InitializeService(nameof(GetUserPassword));
                ValidateEntity(request);
                var result = BL.GetUserPassword(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse<string>>(ex, nameof(GetUserPassword));
                return result;
            }
        }

        public BaseResponse<string> GetRunningInfo(FireInstanceIdRequest request)
        {
            try
            {
                InitializeService(nameof(GetRunningInfo));
                var result = DeamonBL.GetRunningInfo(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse<string>>(ex, nameof(GetRunningInfo));
                return result;
            }
        }

        public BaseResponse UpsertJobProperty(UpsertJobPropertyRequest request)
        {
            try
            {
                InitializeService(nameof(UpsertJobProperty));
                var result = DeamonBL.UpsertJobProperty(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse>(ex, nameof(UpsertJobProperty));
                return result;
            }
        }

        public BaseResponse<string> ReloadMonitor()
        {
            try
            {
                InitializeService(nameof(ReloadMonitor));
                // var result = BL.Reload();
                return null;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse<string>>(ex, nameof(ReloadMonitor));
                return result;
            }
        }

        public BaseResponse<List<string>> GetMonitorHooks()
        {
            try
            {
                InitializeService(nameof(GetMonitorHooks));
                var result = DeamonBL.GetMonitorHooks();
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse<List<string>>>(ex, nameof(GetMonitorHooks));
                return result;
            }
        }

        public BaseResponse<List<MonitorItem>> GetMonitorActions(GetMonitorActionsRequest request)
        {
            try
            {
                InitializeService(nameof(GetMonitorActions));
                var result = BL.GetMonitorActions(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse<List<MonitorItem>>>(ex, nameof(GetMonitorActions));
                return result;
            }
        }

        public BaseResponse<MonitorActionMedatada> GetMonitorActionMedatada()
        {
            try
            {
                InitializeService(nameof(GetMonitorActionMedatada));
                var result = BL.GetMonitorActionMedatada().Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse<MonitorActionMedatada>>(ex, nameof(GetMonitorActionMedatada));
                return result;
            }
        }

        public BaseResponse<List<string>> GetMonitorEvents()
        {
            try
            {
                InitializeService(nameof(GetMonitorEvents));
                var result = DeamonBL.GetMonitorEvents();
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse<List<string>>>(ex, nameof(GetMonitorEvents));
                return result;
            }
        }

        public BaseResponse AddMonitor(AddMonitorRequest request)
        {
            try
            {
                InitializeService(nameof(AddMonitor));
                var result = BL.AddMonitor(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse>(ex, nameof(AddMonitor));
                return result;
            }
        }

        public BaseResponse DeleteMonitor(GetByIdRequest request)
        {
            try
            {
                InitializeService(nameof(DeleteMonitor));
                var result = BL.DeleteMonitor(request).Result;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandleException<BaseResponse>(ex, nameof(DeleteMonitor));
                return result;
            }
        }

        public BaseResponse<bool> RemoveJob(JobOrTriggerKey request)
        {
            throw new NotImplementedException();
        }

        public GetAllJobsResponse GetAllJobs()
        {
            throw new NotImplementedException();
        }
    }
}

////public ResponseType FunctionName(xxx request)
////{
////    try
////    {
////        InitializeService(nameof(FunctionName));
////        var result = BL.FunctionName(request).Result;
////        return result;
////    }
////    catch (Exception ex)
////    {
////        var result = HandleException<ResponseType>(ex, nameof(FunctionName));
////        return result;
////    }
////}