﻿using Microsoft.AspNetCore.Mvc;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Service.API;
using Planar.Validation.Attributes;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Planar.Controllers
{
    [ApiController]
    [Route("job")]
    public class JobController : BaseController<JobDomain>
    {
        public JobController(JobDomain bl) : base(bl)
        {
        }

        [HttpPost("planar")]
        [SwaggerOperation(OperationId = "post_job_planar_add", Description = "Add new planar job", Summary = "Add Planar Job")]
        [JsonConsumes]
        [CreatedResponse(typeof(JobIdResponse))]
        [BadRequestResponse]
        [ConflictResponse]
        public async Task<ActionResult<JobIdResponse>> AddPlanar([FromBody] SetJobRequest<PlanarJobProperties> request)
        {
            var result = await BusinesLayer.Add(request);
            return CreatedAtAction(nameof(Get), result, result);
        }

        [HttpPut("planar")]
        [SwaggerOperation(OperationId = "put_job_planar", Description = "update existing planar job", Summary = "Update Planar Job")]
        [JsonConsumes]
        [CreatedResponse(typeof(JobIdResponse))]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<JobIdResponse>> UpdatePlanar([FromBody] UpdateJobRequest<PlanarJobProperties> request)
        {
            var result = await BusinesLayer.Update(request);
            return CreatedAtAction(nameof(Get), result, result);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost("folder")]
        [SwaggerOperation(OperationId = "post_job_folder", Description = "", Summary = "")]
        [JsonConsumes]
        [CreatedResponse(typeof(JobIdResponse))]
        [BadRequestResponse]
        [ConflictResponse]
        public async Task<ActionResult<JobIdResponse>> AddByFolder([FromBody] SetJobFoldeRequest request)
        {
            var result = await BusinesLayer.AddByFolder(request);
            return CreatedAtAction(nameof(Get), result, result);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPut("folder")]
        [SwaggerOperation(OperationId = "put_job_folder", Description = "", Summary = "")]
        [JsonConsumes]
        [CreatedResponse(typeof(JobIdResponse))]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<JobIdResponse>> UpdateByFolder([FromBody] UpdateJobFolderRequest request)
        {
            var result = await BusinesLayer.UpdateByFolder(request);
            return CreatedAtAction(nameof(Get), result, result);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("available-jobs")]
        [SwaggerOperation(OperationId = "put_job_folder", Description = "", Summary = "")]
        [OkJsonResponse(typeof(List<AvailableJobToAdd>))]
        public async Task<ActionResult<List<AvailableJobToAdd>>> GetAvailableJobsToAdd()
        {
            var result = await BusinesLayer.GetAvailableJobsToAdd();
            return Ok(result);
        }

        [HttpGet]
        [SwaggerOperation(OperationId = "get_job", Description = "Get all jobs", Summary = "Get All Jobs")]
        [OkJsonResponse(typeof(List<JobRowDetails>))]
        public async Task<ActionResult<List<JobRowDetails>>> GetAll([FromQuery] GetAllJobsRequest request)
        {
            var result = await BusinesLayer.GetAll(request);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(OperationId = "delete_job_id", Description = "Delete job", Summary = "Delete Job")]
        [NoContentResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<IActionResult> Remove([FromRoute][Required] string id)
        {
            await BusinesLayer.Remove(id);
            return NoContent();
        }

        [HttpGet("{id}")]
        [SwaggerOperation(OperationId = "get_job_id", Description = "Get job details by id", Summary = "Get Job By Id")]
        [OkJsonResponse(typeof(JobDetails))]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<JobDetails>> Get([FromRoute][Required] string id)
        {
            var result = await BusinesLayer.Get(id);
            return Ok(result);
        }

        [HttpGet("nextRunning/{id}")]
        [SwaggerOperation(OperationId = "get_job_nextRunning_id", Description = "Get the next running date & time of job", Summary = "Get Next Running Date")]
        [OkTextResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<string>> GetNextRunning([FromRoute][Required] string id)
        {
            var result = await BusinesLayer.GetNextRunning(id);
            return Ok(result);
        }

        [HttpGet("prevRunning/{id}")]
        [SwaggerOperation(OperationId = "get_job_prevRunning_id", Description = "Get the previous running date & time of job", Summary = "Get Previous Running Date")]
        [OkTextResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<string>> GetPreviousRunning([FromRoute][Required] string id)
        {
            var result = await BusinesLayer.GetPreviousRunning(id);
            return Ok(result);
        }

        [HttpPost("data")]
        [SwaggerOperation(OperationId = "post_job_data", Description = "Update job data", Summary = "Update Job Data")]
        [JsonConsumes]
        [CreatedResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<IActionResult> AddData([FromBody] JobOrTriggerDataRequest request)
        {
            await BusinesLayer.UpsertData(request, JobDomain.UpsertMode.Add);
            return CreatedAtAction(nameof(Get), new { request.Id }, null);
        }

        [HttpPut("data")]
        [SwaggerOperation(OperationId = "put_job_data", Description = "Update job data", Summary = "Update Job Data")]
        [JsonConsumes]
        [CreatedResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<IActionResult> UpdateData([FromBody] JobOrTriggerDataRequest request)
        {
            await BusinesLayer.UpsertData(request, JobDomain.UpsertMode.Update);
            return CreatedAtAction(nameof(Get), new { request.Id }, null);
        }

        [HttpDelete("{id}/data/{key}")]
        [SwaggerOperation(OperationId = "delete_job_id_data_key", Description = "Delete job data", Summary = "Delete Job Data")]
        [NoContentResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<IActionResult> RemoveData([FromRoute][Required] string id, [FromRoute][Required] string key)
        {
            await BusinesLayer.RemoveData(id, key);
            return NoContent();
        }

        [HttpPost("invoke")]
        [SwaggerOperation(OperationId = "post_job_invoke", Description = "Invoke job", Summary = "Invoke Job")]
        [JsonConsumes]
        [AcceptedContentResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<IActionResult> Invoke([FromBody] InvokeJobRequest request)
        {
            await BusinesLayer.Invoke(request);
            return Accepted();
        }

        [HttpPost("pause")]
        [SwaggerOperation(OperationId = "post_job_pause", Description = "Pause job", Summary = "Pause Job")]
        [JsonConsumes]
        [AcceptedContentResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<IActionResult> Pause([FromBody] JobOrTriggerKey request)
        {
            await BusinesLayer.Pause(request);
            return Accepted();
        }

        [HttpPost("pauseAll")]
        [SwaggerOperation(OperationId = "post_job_pauseall", Description = "Pause all jobs", Summary = "Pause All Jobs")]
        [AcceptedContentResponse]
        public async Task<IActionResult> PauseAll()
        {
            await BusinesLayer.PauseAll();
            return Accepted();
        }

        [HttpPost("resume")]
        [SwaggerOperation(OperationId = "post_job_resume", Description = "Resume job", Summary = "Resume Job")]
        [JsonConsumes]
        [AcceptedContentResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<IActionResult> Resume([FromBody] JobOrTriggerKey request)
        {
            await BusinesLayer.Resume(request);
            return Accepted();
        }

        [HttpPost("resumeAll")]
        [SwaggerOperation(OperationId = "post_job_resumeall", Description = "Resume all jobs", Summary = "Resume All Jobs")]
        [AcceptedContentResponse]
        public async Task<IActionResult> ResumeAll()
        {
            await BusinesLayer.ResumeAll();
            return Accepted();
        }

        [HttpPost("stop")]
        [SwaggerOperation(OperationId = "post_job_stop", Description = "Stop running job", Summary = "Stop Job")]
        [JsonConsumes]
        [AcceptedContentResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<bool>> Stop([FromBody] FireInstanceIdRequest request)
        {
            await BusinesLayer.Stop(request);
            return Accepted();
        }

        [HttpGet("{id}/settings")]
        [SwaggerOperation(OperationId = "gat_job_id_settings", Description = "Get job settings", Summary = "Get Job Settings")]
        [OkJsonResponse(typeof(IEnumerable<KeyValueItem>))]
        [BadRequestResponse]
        public async Task<ActionResult<IEnumerable<KeyValueItem>>> GetSettings([FromRoute][Required] string id)
        {
            var result = await BusinesLayer.GetSettings(id);
            return Ok(result);
        }

        [HttpGet("running/{instanceId}")]
        [SwaggerOperation(OperationId = "get_job_running_instanceid", Description = "Get runnng job info", Summary = "Get Runnng Job Info")]
        [OkJsonResponse(typeof(RunningJobDetails))]
        [BadRequestResponse]
        public async Task<ActionResult<RunningJobDetails>> GetAllRunning([FromRoute][Required] string instanceId)
        {
            var result = await BusinesLayer.GetRunning(instanceId);
            return Ok(result);
        }

        [HttpGet("running")]
        [SwaggerOperation(OperationId = "get_job_running", Description = "Gat all running jobs", Summary = "Gat All Running Jobs")]
        [OkJsonResponse(typeof(List<RunningJobDetails>))]
        public async Task<ActionResult<List<RunningJobDetails>>> GetRunning()
        {
            var result = await BusinesLayer.GetRunning();
            return Ok(result);
        }

        [HttpGet("runningData/{instanceId}")]
        [SwaggerOperation(OperationId = "get_job_runningData_instanceid", Description = "Get running job log & exception", Summary = "Get Running Job Data")]
        [OkJsonResponse(typeof(GetRunningDataResponse))]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<GetRunningDataResponse>> GetRunningData([FromRoute][Required] string instanceId)
        {
            var result = await BusinesLayer.GetRunningData(instanceId);
            return Ok(result);
        }

        [HttpGet("jobfile/{name}")]
        [SwaggerOperation(OperationId = "get_job_jobfile_name", Description = "Get JobFile.yml template", Summary = "Get JobFile.yml Template")]
        [OkYmlResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public ActionResult<string> GetJobFileTemplate([Required][FromRoute] string name)
        {
            var result = BusinesLayer.GetJobFileTemplate(name);
            return Ok(result);
        }

        [HttpGet("jobfiles")]
        [SwaggerOperation(OperationId = "get_job_jobfiles", Description = "Get list of all job files templates", Summary = "Get All Job Files Templates")]
        [OkJsonResponse(typeof(IEnumerable<string>))]
        public ActionResult<IEnumerable<string>> GetJobFileTemplates()
        {
            var result = BusinesLayer.GetJobFileTemplates();
            return Ok(result);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("testStatus/{id}")]
        [SwaggerOperation(OperationId = "get_job_teststatus_id", Description = "", Summary = "")]
        [OkJsonResponse(typeof(GetTestStatusResponse))]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<GetTestStatusResponse>> GetTestStatus([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.GetTestStatus(id);
            return Ok(result);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("{id}/lastInstanceId")]
        [SwaggerOperation(OperationId = "get_job_id_lastinstanceid", Description = "", Summary = "")]
        [OkJsonResponse(typeof(LastInstanceId))]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<LastInstanceId>> GetLastInstanceId([FromRoute][Required] string id, [FromQuery] DateTime invokeDate)
        {
            var result = await BusinesLayer.GetLastInstanceId(id, invokeDate);
            return Ok(result);
        }
    }
}