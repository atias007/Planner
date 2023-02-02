﻿using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.Entities;
using RestSharp;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("history")]
    public class HistoryCliActions : BaseCliAction<HistoryCliActions>
    {
        [Action("get")]
        public static async Task<CliActionResponse> GetHistoryById(CliGetByIdRequest request)
        {
            var restRequest = new RestRequest("history/{id}", Method.Get)
               .AddParameter("id", request.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<CliJobInstanceLog>(restRequest);
            return new CliActionResponse(result, serializeObj: result.Data);
        }

        [Action("ls")]
        [Action("list")]
        public static async Task<CliActionResponse> GetHistory(CliGetHistoryRequest request)
        {
            var restRequest = new RestRequest("history", Method.Get);
            if (request.Rows > 0)
            {
                restRequest.AddQueryParameter("rows", request.Rows);
            }

            if (request.FromDate > DateTime.MinValue)
            {
                restRequest.AddQueryParameter("fromDate", request.FromDate);
            }

            if (request.ToDate > DateTime.MinValue)
            {
                restRequest.AddQueryParameter("toDate", request.ToDate);
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                restRequest.AddQueryParameter("status", request.Status);
            }

            if (!string.IsNullOrEmpty(request.JobId))
            {
                restRequest.AddQueryParameter("jobid", request.JobId);
            }

            if (!string.IsNullOrEmpty(request.JobGroup))
            {
                restRequest.AddQueryParameter("jobgroup", request.JobGroup);
            }

            restRequest.AddQueryParameter("ascending", request.Ascending);

            var result = await RestProxy.Invoke<List<CliJobInstanceLog>>(restRequest);
            var table = CliTableExtensions.GetTable(result.Data);
            return new CliActionResponse(result, table);
        }

        [Action("data")]
        public static async Task<CliActionResponse> GetHistoryDataById(CliGetByIdRequest request)
        {
            var restRequest = new RestRequest("history/{id}/data", Method.Get)
               .AddParameter("id", request.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<string>(restRequest);
            return new CliActionResponse(result, serializeObj: result.Data);
        }

        [Action("log")]
        public static async Task<CliActionResponse> GetHistoryLogById(CliGetByIdRequest request)
        {
            var restRequest = new RestRequest("history/{id}/log", Method.Get)
               .AddParameter("id", request.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<string>(restRequest);
            return new CliActionResponse(result, serializeObj: result.Data);
        }

        [Action("ex")]
        public static async Task<CliActionResponse> GetHistoryExceptionById(CliGetByIdRequest request)
        {
            var restRequest = new RestRequest("history/{id}/exception", Method.Get)
               .AddParameter("id", request.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<string>(restRequest);
            return new CliActionResponse(result, serializeObj: result.Data);
        }

        [Action("last")]
        public static async Task<CliActionResponse> GetLastHistoryCallForJob(CliGetLastHistoryCallForJobRequest request)
        {
            var restRequest = new RestRequest("history/last", Method.Get)
                .AddQueryParameter("lastDays", request.LastDays);

            var result = await RestProxy.Invoke<List<CliJobInstanceLog>>(restRequest);
            var table = CliTableExtensions.GetTable(result.Data);
            return new CliActionResponse(result, table);
        }

        [Action("count")]
        public static async Task<CliActionResponse> GetHistoryCount(CliGetHistoryCountRequest request)
        {
            var restRequest = new RestRequest("history/count", Method.Get)
                .AddQueryParameter("hours", request.Hours);

            var result = await RestProxy.Invoke<CounterResponse>(restRequest);
            if (!result.IsSuccessful || result.Data == null)
            {
                return new CliActionResponse(result);
            }

            var counter = result.Data.Counter;

            AnsiConsole.Write(new BarChart()
                .Width(60)
                .Label($"[grey54 bold]history status count for last {request.Hours} hours[/]")
                .LeftAlignLabel()
                .AddItem(counter[0].Label, counter[0].Count, Color.Gold1)
                .AddItem(counter[1].Label, counter[1].Count, Color.Green)
                .AddItem(counter[2].Label, counter[2].Count, Color.Red1));

            return CliActionResponse.Empty;
        }
    }
}