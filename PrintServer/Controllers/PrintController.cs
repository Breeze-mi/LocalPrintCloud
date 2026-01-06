using Microsoft.AspNetCore.Mvc;
using PrintServer.Models;
using PrintServer.Services;

namespace PrintServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PrintController : ControllerBase
{
    private readonly IPrintQueueService _printQueue;
    private readonly IJobStore _jobStore;
    private readonly ILogger<PrintController> _logger;

    public PrintController(
        IPrintQueueService printQueue,
        IJobStore jobStore,
        ILogger<PrintController> logger)
    {
        _printQueue = printQueue;
        _jobStore = jobStore;
        _logger = logger;
    }

    [HttpPost]
    public ActionResult<PrintResponse> Print([FromBody] PrintRequest request)
    {
        try
        {
            // 验证请求参数
            if (request == null)
            {
                return BadRequest(new { error = "请求参数为空" });
            }

            if (string.IsNullOrEmpty(request.FileId))
            {
                return BadRequest(new { error = "FileId 不能为空" });
            }

            if (string.IsNullOrEmpty(request.PrinterName))
            {
                return BadRequest(new { error = "PrinterName 不能为空" });
            }

            // 验证文件是否存在
            if (!System.IO.File.Exists(request.FileId))
            {
                return BadRequest(new { error = $"文件不存在: {request.FileId}" });
            }

            var job = new PrintJob
            {
                FileId = request.FileId,
                FileName = Path.GetFileName(request.FileId),
                PrinterName = request.PrinterName,
                Copies = request.Copies,
                Options = request.Options,
                Status = PrintJobStatus.Pending
            };

            _jobStore.AddJob(job);
            _printQueue.EnqueueJob(job);

            _logger.LogInformation($"打印任务已创建: {job.JobId}, 文件: {job.FileName}, 打印机: {job.PrinterName}");

            return Ok(new PrintResponse
            {
                JobId = job.JobId,
                Status = job.Status.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建打印任务失败");
            return BadRequest(new { error = ex.Message });
        }
    }
}
