﻿using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Planar.API.Common.Entities;
using Planar.Service.API.Helpers;
using Planar.Service.Model;
using Planar.Service.Model.DataObjects;
using Quartz;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.Data
{
    public class HistoryData : BaseDataLayer
    {
        public HistoryData(PlanarContext context) : base(context)
        {
        }

        public IQueryable<JobInstanceLog> GetHistoryData()
        {
            return _context.JobInstanceLogs.AsQueryable();
        }

        public async Task<List<JobInstanceLog>> GetLastHistoryCallForJob(object parameters)
        {
            using var conn = _context.Database.GetDbConnection();
            var cmd = new CommandDefinition(
                commandText: "dbo.GetLastHistoryCallForJob",
                commandType: CommandType.StoredProcedure,
                parameters: parameters);

            var data = await conn.QueryAsync<JobInstanceLog>(cmd);
            return data.ToList();
        }

        public IQueryable<JobInstanceLog> GetHistory(int key)
        {
            return _context.JobInstanceLogs.Where(l => l.Id == key);
        }

        public IQueryable<JobInstanceLog> GetHistory(GetHistoryRequest request)
        {
            var query = _context.JobInstanceLogs.AsQueryable();

            if (request.FromDate.HasValue)
            {
                query = query.Where(l => l.StartDate >= request.FromDate);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(l => l.StartDate < request.ToDate);
            }

            if (request.Status != null)
            {
                query = query.Where(l => l.Status == (int)request.Status);
            }

            if (!string.IsNullOrEmpty(request.JobId))
            {
                query = query.Where(l => l.JobId == request.JobId);
            }

            if (!string.IsNullOrEmpty(request.JobGroup))
            {
                query = query.Where(l => l.JobGroup == request.JobGroup);
            }

            if (!string.IsNullOrEmpty(request.JobType))
            {
                query = query.Where(l => l.JobType == request.JobType);
            }

            if (request.Ascending)
            {
                query = query.OrderBy(l => l.StartDate);
            }
            else
            {
                query = query.OrderByDescending(l => l.StartDate);
            }

            query = query.Take(request.Rows.GetValueOrDefault());
            return query;
        }

        public async Task<string> GetHistoryDataById(int id)
        {
            var result = await _context.JobInstanceLogs
                .Where(l => l.Id == id)
                .Select(l => l.Data)
                .FirstOrDefaultAsync();

            return result;
        }

        public async Task<string> GetHistoryLogById(int id)
        {
            var result = await _context.JobInstanceLogs
                .Where(l => l.Id == id)
                .Select(l => l.Log)
                .FirstOrDefaultAsync();

            return result;
        }

        public async Task<string> GetHistoryExceptionById(int id)
        {
            var result = await _context.JobInstanceLogs
                .Where(l => l.Id == id)
                .Select(l => l.Exception)
                .FirstOrDefaultAsync();

            return result;
        }

        public async Task<JobInstanceLog> GetHistoryById(int id)
        {
            var result = await _context.JobInstanceLogs
                .Where(l => l.Id == id)
                .FirstOrDefaultAsync();

            return result;
        }

        public async Task<bool> IsHistoryExists(int id)
        {
            var result = await _context.JobInstanceLogs
                .AnyAsync(l => l.Id == id);

            return result;
        }

        public async Task CreateJobInstanceLog(JobInstanceLog log)
        {
            _context.JobInstanceLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateHistoryJobRunLog(JobInstanceLog log)
        {
            var parameters = new
            {
                log.InstanceId,
                log.Status,
                log.StatusTitle,
                log.EndDate,
                log.Duration,
                log.EffectedRows,
                log.Log,
                log.Exception,
                log.IsStopped
            };

            var cmd = new CommandDefinition(
                commandText: "dbo.UpdateJobInstanceLog",
                commandType: CommandType.StoredProcedure,
                parameters: parameters);

            await _context.Database.GetDbConnection().ExecuteAsync(cmd);
        }

        public async Task PersistJobInstanceData(JobInstanceLog log)
        {
            var parameters = new
            {
                log.InstanceId,
                log.Log,
                log.Exception,
                log.Duration,
            };

            var cmd = new CommandDefinition(
                commandText: "dbo.PersistJobInstanceLog",
                commandType: CommandType.StoredProcedure,
                parameters: parameters);

            await _context.Database.GetDbConnection().ExecuteAsync(cmd);
        }

        public async Task SetJobInstanceLogStatus(string instanceId, StatusMembers status)
        {
            var paramInstanceId = new SqlParameter("@InstanceId", instanceId);
            var paramStatus = new SqlParameter("@Status", (int)status);
            var paramTitle = new SqlParameter("@StatusTitle", status.ToString());

            await _context.Database.ExecuteSqlRawAsync($"dbo.SetJobInstanceLogStatus @InstanceId,  @Status, @StatusTitle", paramInstanceId, paramStatus, paramTitle);
        }

        public async Task<LastInstanceId> GetLastInstanceId(JobKey jobKey, DateTime invokeDateTime)
        {
            var result = await _context.JobInstanceLogs
                .Where(l => l.JobName == jobKey.Name && l.JobGroup == jobKey.Group && l.StartDate >= invokeDateTime && l.TriggerId == Consts.ManualTriggerId)
                .Select(l => new LastInstanceId
                {
                    InstanceId = l.InstanceId,
                    LogId = l.Id,
                })
                .FirstOrDefaultAsync();

            return result;
        }

        public async Task<GetTestStatusResponse> GetTestStatus(int id)
        {
            var result = await _context.JobInstanceLogs
                .Where(l => l.Id == id)
                .Select(l => new GetTestStatusResponse
                {
                    EffectedRows = l.EffectedRows,
                    Status = l.Status,
                    Duration = l.Duration
                })
                .FirstOrDefaultAsync();

            return result;
        }

        public async Task<HistoryStatusDto> GetHistoryCounter(int hours)
        {
            var parameters = new { Hours = hours };
            var definition = new CommandDefinition(
                commandText: "[Statistics].[StatusCounter]",
                parameters: parameters,
                commandType: CommandType.StoredProcedure);

            var result = await _context.Database.GetDbConnection()
                .QueryFirstOrDefaultAsync<HistoryStatusDto>(definition);

            return result;
        }
    }
}