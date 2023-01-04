﻿using Microsoft.AspNetCore.Mvc;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Service.API;
using Planar.Validation.Attributes;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Planar.Controllers
{
    [Route("monitor")]
    public class MonitorController : BaseController<MonitorDomain>
    {
        public MonitorController(MonitorDomain bl) : base(bl)
        {
        }

        [HttpGet]
        [SwaggerOperation(OperationId = "get_monitor", Description = "Get all monitors", Summary = "Get All Monitors")]
        [OkJsonResponse(typeof(List<MonitorItem>))]
        public async Task<ActionResult<List<MonitorItem>>> GetAll()
        {
            var result = await BusinesLayer.GetAll();
            return Ok(result);
        }

        [HttpGet("byJob/{jobId}")]
        [SwaggerOperation(OperationId = "get_monitor_byjob_id", Description = "Get monitor by job key or id", Summary = "Get Monitor By Job")]
        [OkJsonResponse(typeof(List<MonitorItem>))]
        [BadRequestResponse]
        public async Task<ActionResult<List<MonitorItem>>> GetByJob([FromRoute][Required] string jobId)
        {
            var result = await BusinesLayer.GetByJob(jobId);
            return Ok(result);
        }

        [HttpGet("byGroup/{group}")]
        [SwaggerOperation(OperationId = "get_monitor_bygroup_group", Description = "Get monitor by job group", Summary = "Get Monitor By Group")]
        [OkJsonResponse(typeof(List<MonitorItem>))]
        [BadRequestResponse]
        public async Task<ActionResult<List<MonitorItem>>> GetByGroup([FromRoute][Required] string group)
        {
            var result = await BusinesLayer.GetByGroup(group);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [SwaggerOperation(OperationId = "get_monitor_id", Description = "Get monitor by id", Summary = "Get Monitor")]
        [OkJsonResponse(typeof(MonitorItem))]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<MonitorItem>> GetById([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.GetById(id);
            return Ok(result);
        }

        [HttpGet("events")]
        [SwaggerOperation(OperationId = "get_monitor_events", Description = "Get all monitor events type", Summary = "Get All Monitor Events")]
        [OkJsonResponse(typeof(List<LovItem>))]
        public ActionResult<List<LovItem>> GetEvents()
        {
            var result = BusinesLayer.GetEvents();
            return Ok(result);
        }

        [HttpPost]
        [SwaggerOperation(OperationId = "post_monitor", Description = "Add monitor", Summary = "Add Monitor")]
        [JsonConsumes]
        [CreatedResponse(typeof(EntityIdResponse))]
        [BadRequestResponse]
        [ConflictResponse]
        public async Task<ActionResult<EntityIdResponse>> Add([FromBody] AddMonitorRequest request)
        {
            var result = await BusinesLayer.Add(request);
            return CreatedAtAction(nameof(GetById), new { id = result }, new EntityIdResponse(result));
        }

        [HttpPut]
        [SwaggerOperation(OperationId = "put_monitor", Description = "Update monitor", Summary = "Update Monitor")]
        [JsonConsumes]
        [NoContentResponse]
        [BadRequestResponse]
        [ConflictResponse]
        [NotFoundResponse]
        public async Task<IActionResult> Update([FromBody] UpdateMonitorRequest request)
        {
            await BusinesLayer.Update(request);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(OperationId = "delete_monitor_id", Description = "Delete monitor", Summary = "Delete Monitor")]
        [BadRequestResponse]
        [NoContentResponse]
        [NotFoundResponse]
        public async Task<ActionResult> Delete([FromRoute][Id] int id)
        {
            await BusinesLayer.Delete(id);
            return NoContent();
        }

        [HttpPatch]
        [SwaggerOperation(OperationId = "patch_monitor", Description = "Update monitor single property", Summary = "Partial Update Monitor")]
        [JsonConsumes]
        [BadRequestResponse]
        [NoContentResponse]
        [BadRequestResponse]
        [ConflictResponse]
        [NotFoundResponse]
        public async Task<ActionResult> UpdatePartial([FromBody] UpdateEntityRecord request)
        {
            await BusinesLayer.PartialUpdateMonitor(request);
            return NoContent();
        }

        [HttpPost("reload")]
        [SwaggerOperation(OperationId = "post_monitor_reload", Description = "Refresh monitor cache", Summary = "Refresh Monitor Cache")]
        [OkJsonResponse(typeof(string))]
        public async Task<ActionResult<string>> Reload()
        {
            var result = await BusinesLayer.Reload();
            return Ok(result);
        }

        [HttpGet("hooks")]
        [SwaggerOperation(OperationId = "get_monitor_hooks", Description = "Get all monitor hooks", Summary = "Get Monitor Hooks")]
        [OkJsonResponse(typeof(string))]
        public ActionResult<List<string>> GetHooks()
        {
            var result = BusinesLayer.GetHooks();
            return Ok(result);
        }
    }
}