﻿using Planar.API.Common.Entities;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public class TraceDomain : BaseBL<TraceDomain, TraceData>
    {
        public TraceDomain(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public IQueryable<Model.Trace> GetTraceData()
        {
            return DataLayer.GetTraceData();
        }

        public Model.Trace GetTrace(int key)
        {
            var trace = DataLayer.GetTrace(key);

            if (trace == null)
            {
                throw new RestNotFoundException();
            }

            return trace;
        }

        public async Task<List<LogDetails>> Get(GetTraceRequest request)
        {
            if (request.Rows.GetValueOrDefault() == 0) { request.Rows = 50; }
            var result = await DataLayer.GetTrace(request);
            return result;
        }

        public async Task<string> GetException(int id)
        {
            var result = await DataLayer.GetTraceException(id);

            if (result == null)
            {
                if (await DataLayer.IsTraceExists(id) == false)
                {
                    throw new RestNotFoundException($"trace with id {id} not found");
                }
            }

            return result;
        }

        public async Task<string> GetProperties(int id)
        {
            var result = await DataLayer.GetTraceProperties(id);

            if (result == null)
            {
                if (await DataLayer.IsTraceExists(id) == false)
                {
                    throw new RestNotFoundException($"trace with id {id} not found");
                }
            }

            return result;
        }
    }
}