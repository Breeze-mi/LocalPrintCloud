using Microsoft.AspNetCore.Mvc;
using PrintServer.Models;
using PrintServer.Services;

namespace PrintServer.Controllers;

[ApiController]
[Route("api")]
public class JobsController : ControllerBase
{
    private readonly IJobStore _jobStore;

    public JobsController(IJobStore jobStore)
    {
        _jobStore = jobStore;
    }

    [HttpGet("status/{jobId}")]
    public ActionResult<PrintJob> GetStatus(string jobId)
    {
        var job = _jobStore.GetJob(jobId);
        if (job == null)
        {
            return NotFound(new { error = "任务不存在" });
        }
        return Ok(job);
    }

    [HttpGet("jobs")]
    public ActionResult<List<PrintJob>> GetAllJobs()
    {
        var jobs = _jobStore.GetAllJobs();
        return Ok(jobs);
    }
}
