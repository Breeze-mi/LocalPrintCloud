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
    private readonly IPrintStatisticsService _statisticsService;
    private readonly ILogger<PrintController> _logger;

    public PrintController(
        IPrintQueueService printQueue,
        IJobStore jobStore,
        IPrintStatisticsService statisticsService,
        ILogger<PrintController> logger)
    {
        _printQueue = printQueue;
        _jobStore = jobStore;
        _statisticsService = statisticsService;
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

            // 支持单文件（向后兼容）和多文件
            var fileIds = request.FileIds != null && request.FileIds.Count > 0 
                ? request.FileIds 
                : new List<string>();

            if (fileIds.Count == 0)
            {
                return BadRequest(new { error = "FileIds 不能为空" });
            }

            if (string.IsNullOrEmpty(request.PrinterName))
            {
                return BadRequest(new { error = "PrinterName 不能为空" });
            }

            // 验证所有文件是否存在
            foreach (var fileId in fileIds)
            {
                if (!System.IO.File.Exists(fileId))
                {
                    return BadRequest(new { error = $"文件不存在: {fileId}" });
                }
            }

            // 计算打印统计
            var statistics = _statisticsService.CalculateStatistics(fileIds, request.Options, request.Copies);

            // 生成文件名
            var fileName = fileIds.Count == 1 
                ? Path.GetFileName(fileIds[0])
                : $"{fileIds.Count} 个文件" + (request.MergeFiles ? " (合并)" : "");

            var job = new PrintJob
            {
                FileIds = fileIds,
                FileName = fileName,
                PrinterName = request.PrinterName,
                Copies = request.Copies,
                Options = request.Options,
                Status = PrintJobStatus.Pending,
                Statistics = statistics,
                MergeFiles = request.MergeFiles
            };

            _jobStore.AddJob(job);
            _printQueue.EnqueueJob(job);

            _logger.LogInformation($"打印任务已创建: {job.JobId}, 文件: {job.FileName}, 打印机: {job.PrinterName}, 预估页数: {statistics.TotalPages}");

            return Ok(new PrintResponse
            {
                JobId = job.JobId,
                Status = job.Status.ToString(),
                Statistics = statistics
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建打印任务失败");
            return BadRequest(new { error = ex.Message });
        }
    }

    // 预估打印费用（不创建任务）
    [HttpPost("estimate")]
    public ActionResult<PrintStatistics> EstimateCost([FromBody] PrintRequest request)
    {
        try
        {
            if (request == null || request.FileIds == null || request.FileIds.Count == 0)
            {
                return BadRequest(new { error = "请求参数无效" });
            }

            var statistics = _statisticsService.CalculateStatistics(request.FileIds, request.Options, request.Copies);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "预估费用失败");
            return BadRequest(new { error = ex.Message });
        }
    }
}
