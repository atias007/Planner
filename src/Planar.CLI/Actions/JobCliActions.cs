﻿using FluentValidation;
using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.Entities;
using RestSharp;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Planar.CLI.Actions
{
    [Module("job")]
    public class JobCliActions : BaseCliAction<JobCliActions>
    {
        [Action("add")]
        public static async Task<CliActionResponse> AddJob(CliAddJobRequest request)
        {
            if (request.Filename == ".") { request.Filename = JobFileName; }
            var fi = new FileInfo(request.Filename);
            RestRequest restRequest;

            if (fi.Extension.ToLower() == ".yml")
            {
                if (fi.Exists == false)
                {
                    throw new CliException($"filename '{fi.FullName}' not exist");
                }

                var yml = File.ReadAllText(fi.FullName);
                var prm = GetAddJobRequest(yml);

                restRequest = new RestRequest("job", Method.Post)
                    .AddBody(prm);
            }
            else
            {
                var body = new AddJobFoldeRequest { Folder = request.Filename };
                restRequest = new RestRequest("job/folder", Method.Post)
                    .AddBody(body);
            }

            var result = await RestProxy.Invoke<JobIdResponse>(restRequest);
            return new CliActionResponse(result, message: result.Data?.Id);
        }

        [Action("ls")]
        [Action("list")]
        public static async Task<CliActionResponse> GetAllJobs(CliGetAllJobsRequest request)
        {
            var restRequest = new RestRequest("job", Method.Get);
            var p = AllJobsMembers.AllUserJobs;
            if (request.System) { p = AllJobsMembers.AllSystemJobs; }
            if (request.All) { p = AllJobsMembers.All; }
            restRequest.AddQueryParameter("filter", (int)p);

            var result = await RestProxy.Invoke<List<JobRowDetails>>(restRequest);
            var message = string.Empty;
            CliActionResponse response;
            if (request.Quiet)
            {
                message = string.Join('\n', result.Data?.Select(r => r.Id));
                response = new CliActionResponse(result, message);
            }
            else
            {
                var table = CliTableExtensions.GetTable(result.Data);
                response = new CliActionResponse(result, table);
            }

            return response;
        }

        [Action("get")]
        [Action("inspect")]
        public static async Task<CliActionResponse> GetJobDetails(CliJobOrTriggerKey jobKey)
        {
            var restRequest = new RestRequest("job/{id}", Method.Get)
                .AddParameter("id", jobKey.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<JobDetails>(restRequest);
            var tables = CliTableExtensions.GetTable(result.Data);
            return new CliActionResponse(result, tables);
        }

        [Action("next")]
        public static async Task<CliActionResponse> GetNextRunning(CliJobOrTriggerKey jobKey)
        {
            var restRequest = new RestRequest("job/nextRunning/{id}", Method.Get)
                .AddParameter("id", jobKey.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<DateTime?>(restRequest);
            var message = $"{result?.Data?.ToShortDateString()} {result?.Data?.ToShortTimeString()}";
            return new CliActionResponse(result, message: message);
        }

        [Action("settings")]
        public static async Task<CliActionResponse> GetJobSettings(CliJobOrTriggerKey jobKey)
        {
            var restRequest = new RestRequest("job/{id}/settings", Method.Get)
                .AddParameter("id", jobKey.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<Dictionary<string, string>>(restRequest);
            return new CliActionResponse(result, serializeObj: result.Data);
        }

        [Action("runningex")]
        public static async Task<CliActionResponse> GetRunningExceptions(CliFireInstanceIdRequest request)
        {
            var restRequest = new RestRequest("job/runningData/{instanceId}", Method.Get)
                .AddParameter("instanceId", request.FireInstanceId, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<GetRunningDataResponse>(restRequest);
            if (string.IsNullOrEmpty(result.Data?.Exceptions)) { return new CliActionResponse(result); }

            return new CliActionResponse(result, result.Data?.Exceptions);
        }

        [Action("runninglog")]
        public static async Task<CliActionResponse> GetRunningData(CliFireInstanceIdRequest request)
        {
            var restRequest = new RestRequest("job/runningData/{instanceId}", Method.Get)
                .AddParameter("instanceId", request.FireInstanceId, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<GetRunningDataResponse>(restRequest);
            if (string.IsNullOrEmpty(result.Data?.Log)) { return new CliActionResponse(result); }

            return new CliActionResponse(result, result.Data?.Log);
        }

        [Action("running")]
        public static async Task<CliActionResponse> GetRunningJobs(CliGetRunningJobsRequest request)
        {
            var result = await GetRunningJobsInner(request);

            if (request.Quiet)
            {
                var data = result.Item1?.Select(i => i.FireInstanceId).ToList();
                var sb = new StringBuilder();
                if (data != null)
                {
                    data.ForEach(m => sb.AppendLine(m));
                }

                return new CliActionResponse(result.Item2, message: sb.ToString());
            }

            if (request.Details)
            {
                return new CliActionResponse(result.Item2, serializeObj: result.Item1);
            }

            var table = CliTableExtensions.GetTable(result.Item1);
            return new CliActionResponse(result.Item2, table);
        }

        [Action("invoke")]
        public static async Task<CliActionResponse> InvokeJob(CliInvokeJobRequest request)
        {
            var result = await InvokeJobInner(request);
            return new CliActionResponse(result);
        }

        [Action("pauseall")]
        public static async Task<CliActionResponse> PauseAll()
        {
            var restRequest = new RestRequest("job/pauseAll", Method.Post);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("pause")]
        public static async Task<CliActionResponse> PauseJob(CliJobOrTriggerKey jobKey)
        {
            var restRequest = new RestRequest("job/pause", Method.Post)
                .AddBody(jobKey);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("remove")]
        [Action("delete")]
        public static async Task<CliActionResponse> RemoveJob(CliJobOrTriggerKey jobKey)
        {
            var restRequest = new RestRequest("job/{id}", Method.Delete)
                .AddParameter("id", jobKey.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("resumeall")]
        public static async Task<CliActionResponse> ResumeAll()
        {
            var restRequest = new RestRequest("job/resumeAll", Method.Post);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("resume")]
        public static async Task<CliActionResponse> ResumeJob(CliJobOrTriggerKey jobKey)
        {
            var restRequest = new RestRequest("job/resume", Method.Post)
                .AddBody(jobKey);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("stop")]
        public static async Task<CliActionResponse> StopRunningJob(CliFireInstanceIdRequest request)
        {
            var restRequest = new RestRequest("job/stop", Method.Post)
                .AddBody(request);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("test")]
        public static async Task<CliActionResponse> TestJob(CliInvokeJobRequest request)
        {
            var invokeDate = DateTime.Now.AddSeconds(-1);

            // (1) Invoke job
            var step1 = await TestStep1InvokeJob(request);
            if (step1 != null) { return step1; }

            // (2) Sleep 1 sec
            await Task.Delay(1000);

            // (3) Get instance id
            var step3 = await TestStep2GetInstanceId(request, invokeDate);
            if (step3.Item1 != null) { return step3.Item1; }
            var instanceId = step3.Item2;
            var logId = step3.Item3;

            // (4) Get running info
            var step4 = await TestStep4GetRunningData(instanceId, invokeDate);
            if (step4 != null) { return step4; }

            // (5) Sleep 1 sec
            await Task.Delay(1000);

            // (6) Check log
            var step6 = await TestStep6CheckLog(logId);
            if (step6 != null) { return step6; }
            return CliActionResponse.Empty;
        }

        [Action("data")]
        public static async Task<CliActionResponse> UpsertJobData(CliJobDataRequest request)
        {
            RestResponse result;
            switch (request.Action)
            {
                case JobDataActions.upsert:
                    var prm1 = new JobDataRequest
                    {
                        Id = request.Id,
                        DataKey = request.DataKey,
                        DataValue = request.DataValue
                    };

                    var restRequest1 = new RestRequest("job/data", Method.Post).AddBody(prm1);
                    result = await RestProxy.Invoke(restRequest1);
                    break;

                case JobDataActions.remove:
                    var restRequest2 = new RestRequest("job/{id}/data/{key}", Method.Delete)
                        .AddParameter("id", request.Id, ParameterType.UrlSegment)
                        .AddParameter("key", request.DataKey, ParameterType.UrlSegment);

                    result = await RestProxy.Invoke(restRequest2);
                    break;

                case JobDataActions.clear:
                    var restRequest3 = new RestRequest("job/{id}/allData", Method.Delete)
                        .AddParameter("id", request.Id, ParameterType.UrlSegment);
                    result = await RestProxy.Invoke(restRequest3);
                    break;

                default:
                    throw new ValidationException($"Action {request.Action} is not supported for this command");
            }

            return new CliActionResponse(result);
        }

        [Action("updateprop")]
        public static async Task<CliActionResponse> UpsertJobProperty(CliUpsertJobPropertyRequest request)
        {
            var restRequest = new RestRequest("job/property", Method.Put)
                .AddBody(request);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        public static async Task<string> ChooseJob()
        {
            var restRequest = new RestRequest("job", Method.Get);
            var p = AllJobsMembers.AllUserJobs;
            restRequest.AddQueryParameter("filter", (int)p);
            var result = await RestProxy.Invoke<List<JobRowDetails>>(restRequest);
            if (result.IsSuccessful)
            {
                var jobs = result.Data
                    .OrderBy(d => d.Group)
                    .ThenBy(d => d.Name)
                    .Select(d => $"{d.Group}.{d.Name}")
                    .ToList();

                return AnsiConsole.Prompt(
                     new SelectionPrompt<string>()
                         .Title("[underline]select job to invoke (press enter to select):[/]")
                         .PageSize(10)
                         .MoreChoicesText("[grey](Move up and down to reveal more jobs)[/]")
                         .AddChoices(jobs));
            }
            else
            {
                throw new CliException($"fail to fetch list of jobs. error message: {result.ErrorMessage}");
            }
        }

        private static AddJobRequest GetAddJobRequest(string yaml)
        {
            var deserializer = new DeserializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .Build();
            var request = deserializer.Deserialize<AddJobRequest>(yaml);
            return request;
        }

        internal static async Task<(List<RunningJobDetails>, RestResponse)> GetRunningJobsInner(CliGetRunningJobsRequest request)
        {
            if (request.Iterative && request.Details)
            {
                throw new CliException("running command can't accept both 'iterative' and 'details' parameters");
            }

            RestRequest restRequest;
            RestResponse restResponse;
            List<RunningJobDetails> resultData = null;

            if (string.IsNullOrEmpty(request.FireInstanceId))
            {
                restRequest = new RestRequest("job/running", Method.Get);
                var result = await RestProxy.Invoke<List<RunningJobDetails>>(restRequest);
                resultData = result.Data;
                restResponse = result;
            }
            else
            {
                restRequest = new RestRequest("job/running/{instanceId}", Method.Get)
                    .AddParameter("instanceId", request.FireInstanceId, ParameterType.UrlSegment);
                var result = await RestProxy.Invoke<RunningJobDetails>(restRequest);
                if (result.Data != null)
                {
                    resultData = new List<RunningJobDetails> { result.Data };
                }

                restResponse = result;
            }

            return (resultData, restResponse);
        }

        private static async Task<RestResponse<LastInstanceId>> GetLastInstanceId(string id, DateTime invokeDate)
        {
            // UTC
            var dateParameter = invokeDate.ToString("s", CultureInfo.InvariantCulture);

            var restRequest = new RestRequest("job/{id}/lastInstanceId", Method.Get)
                .AddParameter("id", id, ParameterType.UrlSegment)
                .AddParameter("invokeDate", dateParameter, ParameterType.QueryString);
            var result = await RestProxy.Invoke<LastInstanceId>(restRequest);
            return result;
        }

        private static async Task<RestResponse> InvokeJobInner(CliInvokeJobRequest request)
        {
            var prm = JsonMapper.Map<InvokeJobRequest, CliInvokeJobRequest>(request);
            if (prm.NowOverrideValue == DateTime.MinValue) { prm.NowOverrideValue = null; }

            var restRequest = new RestRequest("job/invoke", Method.Post)
                .AddBody(prm);
            var result = await RestProxy.Invoke(restRequest);
            return result;
        }

        private static async Task<CliActionResponse> TestStep1InvokeJob(CliInvokeJobRequest request)
        {
            // (1) Invoke job
            AnsiConsole.MarkupLine(" [gold3_1][[x]][/] Invoke job...");
            var result = await InvokeJobInner(request);
            if (result.IsSuccessful)
            {
                return null;
            }

            return new CliActionResponse(result);
        }

        private static async Task<(CliActionResponse, string, int)> TestStep2GetInstanceId(CliInvokeJobRequest request, DateTime invokeDate)
        {
            AnsiConsole.Markup(" [gold3_1][[x]][/] Get instance id... ");
            RestResponse<LastInstanceId> instanceId = null;
            for (int i = 0; i < 20; i++)
            {
                instanceId = await GetLastInstanceId(request.Id, invokeDate);
                if (instanceId.IsSuccessful == false)
                {
                    return (new CliActionResponse(instanceId), null, 0);
                }

                if (instanceId.Data != null) break;
                await Task.Delay(1000);
            }

            if (instanceId == null || instanceId.Data == null)
            {
                AnsiConsole.WriteLine();
                throw new CliException("Could not found running instance id");
            }

            AnsiConsole.MarkupLine($"[turquoise2]{instanceId.Data.InstanceId}[/]");
            return (null, instanceId.Data.InstanceId, instanceId.Data.LogId);
        }

        private static async Task<CliActionResponse> TestStep4GetRunningData(string instanceId, DateTime invokeDate)
        {
            var restRequest = new RestRequest("job/running/{instanceId}", Method.Get)
                .AddParameter("instanceId", instanceId, ParameterType.UrlSegment);
            var runResult = await RestProxy.Invoke<RunningJobDetails>(restRequest);

            if (runResult.IsSuccessful == false) { return new CliActionResponse(runResult); }
            Console.WriteLine();
            var sleepTime = 2000;
            while (runResult.Data != null)
            {
                Console.CursorTop -= 1;
                var span = DateTime.Now.Subtract(invokeDate);
                AnsiConsole.MarkupLine($" [gold3_1][[x]][/] Progress: [wheat1]{runResult.Data.Progress}[/]%  |  Effected Row(s): [wheat1]{runResult.Data.EffectedRows.GetValueOrDefault()}  |  Run Time: {CliTableFormat.FormatTimeSpan(span)}[/]     ");
                Thread.Sleep(sleepTime);
                runResult = await RestProxy.Invoke<RunningJobDetails>(restRequest);
                if (runResult.IsSuccessful == false) { break; }
                if (span.TotalMinutes >= 5) { sleepTime = 10000; }
                else if (span.TotalMinutes >= 15) { sleepTime = 20000; }
                else if (span.TotalMinutes >= 30) { sleepTime = 30000; }
            }

            Console.CursorTop -= 1;
            AnsiConsole.Markup($" [gold3_1][[x]][/] Progress: [green]100%[/]  |  ");

            return null;
        }

        private static async Task<CliActionResponse> TestStep6CheckLog(int logId)
        {
            var restTestRequest = new RestRequest("job/testStatus/{id}", Method.Get)
                .AddParameter("id", logId, ParameterType.UrlSegment);
            var status = await RestProxy.Invoke<GetTestStatusResponse>(restTestRequest);

            if (status.IsSuccessful == false) { return new CliActionResponse(status); }
            if (status.Data == null)
            {
                Console.WriteLine();
                throw new CliException($"Could not found log data for log id {logId}");
            }

            var finalSpan = TimeSpan.FromMilliseconds(status.Data.Duration.GetValueOrDefault());
            AnsiConsole.Markup($"Effected Row(s): {status.Data.EffectedRows.GetValueOrDefault()}");
            AnsiConsole.MarkupLine($"  |  Run Time: {CliTableFormat.FormatTimeSpan(finalSpan)}");
            AnsiConsole.Markup(" [gold3_1][[x]][/] ");
            if (status.Data.Status == 0)
            {
                AnsiConsole.Markup("[green]Success[/]");
            }
            else
            {
                AnsiConsole.Markup($"[red]Fail (status {status.Data.Status})[/]");
            }

            Console.WriteLine();
            Console.WriteLine();

            var table = new Table();
            table.AddColumn(new TableColumn(new Markup("[grey54]Get more information by the following commands[/]")));
            table.BorderColor(Color.FromInt32(242));
            table.AddRow($"[grey54]history get[/] [grey62]{logId}[/]");
            table.AddRow($"[grey54]history log[/] [grey62]{logId}[/]");
            table.AddRow($"[grey54]history data[/] [grey62]{logId}[/]");

            if (status.Data.Status == 1)
            {
                table.AddRow($"[grey54]Planar history ex[/] [grey62]{logId}[/]");
            }

            AnsiConsole.Write(table);

            return null;
        }
    }
}