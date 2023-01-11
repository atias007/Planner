﻿using Microsoft.AspNetCore.Mvc;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Service.API;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Planar.Controllers
{
    [Route("trigger")]
    public class TriggerController : BaseController<TriggerDomain>
    {
        public TriggerController(TriggerDomain bl) : base(bl)
        {
        }

        [HttpGet("{triggerId}")]
        [SwaggerOperation(OperationId = "get_trigger_triggerid", Description = "Get trigger by id", Summary = "Get Trigger")]
        [BadRequestResponse]
        [OkJsonResponse(typeof(TriggerRowDetails))]
        public async Task<ActionResult<TriggerRowDetails>> Get([FromRoute][Required] string triggerId)
        {
            var result = await BusinesLayer.Get(triggerId);
            return Ok(result);
        }

        [HttpGet("{jobId}/byjob")]
        [SwaggerOperation(OperationId = "get_trigger_jobid_byjob", Description = "Find triggers by job ", Summary = "Find Triggers By Job")]
        [NotFoundResponse]
        [BadRequestResponse]
        [OkJsonResponse(typeof(TriggerRowDetails))]
        public async Task<ActionResult<TriggerRowDetails>> GetByJob([FromRoute][Required] string jobId)
        {
            var result = await BusinesLayer.GetByJob(jobId);
            return Ok(result);
        }

        [HttpDelete("{triggerId}")]
        [SwaggerOperation(OperationId = "delete_trigger_triggerId", Description = "Delete trigger", Summary = "Delete Trigger")]
        [NotFoundResponse]
        [BadRequestResponse]
        [NoContentResponse]
        public async Task<ActionResult> Delete([FromRoute][Required] string triggerId)
        {
            await BusinesLayer.Delete(triggerId);
            return NoContent();
        }

        [HttpPost("pause")]
        [JsonConsumes]
        [SwaggerOperation(OperationId = "post_trigger_pause", Description = "Pause trigger", Summary = "Pause Trigger")]
        [NotFoundResponse]
        [BadRequestResponse]
        [NoContentResponse]
        public async Task<ActionResult> Pause([FromBody] JobOrTriggerKey request)
        {
            await BusinesLayer.Pause(request);
            return NoContent();
        }

        [HttpPost("resume")]
        [JsonConsumes]
        [SwaggerOperation(OperationId = "post_trigger_resume", Description = "Resume trigger", Summary = "Resume Trigger")]
        [NotFoundResponse]
        [BadRequestResponse]
        [NoContentResponse]
        public async Task<ActionResult> Resume([FromBody] JobOrTriggerKey request)
        {
            await BusinesLayer.Resume(request);
            return NoContent();
        }

        [HttpPost("data")]
        [JsonConsumes]
        [SwaggerOperation(OperationId = "post_trigger_data", Description = "Add trigger data", Summary = "Add Trigger Data")]
        [CreatedResponse]
        [NotFoundResponse]
        [BadRequestResponse]
        public async Task<IActionResult> AddData([FromBody] JobOrTriggerDataRequest request)
        {
            await BusinesLayer.UpsertData(request, JobDomain.UpsertMode.Add);
            return CreatedAtAction(nameof(Get), new { triggerId = request.Id }, null);
        }

        [HttpPut("data")]
        [JsonConsumes]
        [SwaggerOperation(OperationId = "put_trigger_data", Description = "Update trigger data", Summary = "Update Trigger Data")]
        [CreatedResponse]
        [NotFoundResponse]
        [BadRequestResponse]
        public async Task<IActionResult> UpdateData([FromBody] JobOrTriggerDataRequest request)
        {
            await BusinesLayer.UpsertData(request, JobDomain.UpsertMode.Update);
            return CreatedAtAction(nameof(Get), new { triggerId = request.Id }, null);
        }

        [HttpDelete("{id}/data/{key}")]
        [SwaggerOperation(OperationId = "delete_trigger_id_data_key", Description = "Delete trigger data", Summary = "Delete Trigger Data")]
        [NoContentResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<IActionResult> RemoveData([FromRoute][Required] string id, [FromRoute][Required] string key)
        {
            await BusinesLayer.RemoveData(id, key);
            return NoContent();
        }
    }
}