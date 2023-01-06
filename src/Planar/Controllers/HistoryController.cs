﻿using Microsoft.AspNetCore.Mvc;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Service.API;
using Planar.Service.Model;
using Planar.Validation.Attributes;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.Controllers
{
    [Route("history")]
    public class HistoryController : BaseController<HistoryDomain>
    {
        public HistoryController(HistoryDomain bl) : base(bl)
        {
        }

        [HttpGet]
        [SwaggerOperation(OperationId = "get_history", Description = "Get history data by filter", Summary = "Get History")]
        [OkJsonResponse(typeof(List<JobInstanceLogRow>))]
        [BadRequestResponse]
        public async Task<ActionResult<List<JobInstanceLogRow>>> GetHistory([FromQuery] GetHistoryRequest request)
        {
            var result = await BusinesLayer.GetHistory(request);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [SwaggerOperation(OperationId = "get_history_id", Description = "Get history by id", Summary = "Get History By Id")]
        [OkJsonResponse(typeof(JobInstanceLog))]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<JobInstanceLog>> GetHistoryById([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.GetHistoryById(id);
            return Ok(result);
        }

        [HttpGet("{id}/data")]
        [SwaggerOperation(OperationId = "get_history_id_data", Description = "Get only variables data from specific history item", Summary = "Get History Data By Id")]
        [OkTextResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<string>> GetHistoryDataById([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.GetHistoryDataById(id);
            return Ok(result);
        }

        [HttpGet("{id}/log")]
        [SwaggerOperation(OperationId = "get_history_id_log", Description = "Get only log text from specific history item", Summary = "Get History Log By Id")]
        [OkTextResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<string>> GetHistoryLogById([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.GetHistoryLogById(id);
            return Ok(result);
        }

        [HttpGet("{id}/exception")]
        [SwaggerOperation(OperationId = "get_history_id_exception", Description = "Get only exceptions text from specific history item", Summary = "Get History Exceptions By Id")]
        [OkTextResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<string>> GetHistoryExceptionById([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.GetHistoryExceptionById(id);
            return Ok(result);
        }

        [HttpGet("last")]
        [SwaggerOperation(OperationId = "get_history_last", Description = "Get summary of last running of each job", Summary = "Get Last Running Per Job")]
        [OkJsonResponse(typeof(JobInstanceLog))]
        [BadRequestResponse]
        public async Task<ActionResult<List<JobInstanceLog>>> GetLastHistoryCallForJob([FromQuery][UInt] int lastDays)
        {
            var result = await BusinesLayer.GetLastHistoryCallForJob(lastDays);
            return Ok(result);
        }
    }
}