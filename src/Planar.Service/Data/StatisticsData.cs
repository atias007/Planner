﻿using Dapper;
using Microsoft.EntityFrameworkCore;
using Planar.Service.Model;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.Data
{
    public class StatisticsData : BaseDataLayer
    {
        public StatisticsData(PlanarContext context) : base(context)
        {
        }

        public async Task AddCocurentQueueItem(ConcurentQueue item)
        {
            _context.Add(item);
            await SaveChangesAsync();
        }

        public async Task<int> ClearStatisticsTables(int overDays)
        {
            var parameters = new { OverDays = overDays };
            using var conn = _context.Database.GetDbConnection();
            var cmd = new CommandDefinition(
                commandText: "Statistics.ClearStatistics",
                commandType: CommandType.StoredProcedure,
                parameters: parameters);

            return await conn.ExecuteAsync(cmd);
        }

        public async Task<int> SetMaxConcurentExecution()
        {
            using var conn = _context.Database.GetDbConnection();
            var cmd = new CommandDefinition(
                commandText: "Statistics.SetMaxConcurentExecution",
                commandType: CommandType.StoredProcedure);

            return await conn.ExecuteAsync(cmd);
        }

        public async Task<int> SetMaxDurationExecution()
        {
            using var conn = _context.Database.GetDbConnection();
            var cmd = new CommandDefinition(
                commandText: "Statistics.SetMaxDurationExecution",
                commandType: CommandType.StoredProcedure);

            return await conn.ExecuteAsync(cmd);
        }

        public async Task<int> BuildJobStatistics()
        {
            using var conn = _context.Database.GetDbConnection();
            var cmd = new CommandDefinition(
                commandText: "Statistics.BuildJobStatistics",
                commandType: CommandType.StoredProcedure);

            return await conn.ExecuteAsync(cmd);
        }

        public async Task<IEnumerable<JobStatistic>> GetJobStatistics()
        {
            return await _context.JobStatistics
                .AsNoTracking()
                .ToListAsync();
        }

        public IQueryable<JobInstanceLog> GetNullAnomaly()
        {
            return _context.JobInstanceLogs
                .AsNoTracking()
                .Where(j => j.Anomaly == null);
        }

        public void SetAnomaly(IEnumerable<JobInstanceLog> logs)
        {
            foreach (var log in logs)
            {
                _context.Attach(log);
                _context.Entry(log).Property(l => l.Anomaly).IsModified = true;
            }
        }
    }
}