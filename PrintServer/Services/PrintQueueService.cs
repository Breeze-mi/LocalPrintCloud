using System.Threading.Channels;
using PrintServer.Models;

namespace PrintServer.Services;

public class PrintQueueService : IPrintQueueService
{
    private readonly Channel<PrintJob> _queue;
    private readonly IPrinterService _printerService;
    private readonly IJobStore _jobStore;
    private readonly ILogger<PrintQueueService> _logger;
    private readonly IConfiguration _configuration;
    private readonly int _maxRetries;

    public PrintQueueService(
        IPrinterService printerService,
        IJobStore jobStore,
        ILogger<PrintQueueService> logger,
        IConfiguration configuration)
    {
        _queue = Channel.CreateUnbounded<PrintJob>();
        _printerService = printerService;
        _jobStore = jobStore;
        _logger = logger;
        _configuration = configuration;
        _maxRetries = int.Parse(_configuration["MaxRetries"] ?? "3");
    }

    public void EnqueueJob(PrintJob job)
    {
        _queue.Writer.TryWrite(job);
        _logger.LogInformation($"任务已加入队列: {job.JobId}");
    }

    public async Task StartProcessingAsync()
    {
        _logger.LogInformation("打印队列处理已启动");

        await foreach (var job in _queue.Reader.ReadAllAsync())
        {
            await ProcessJobAsync(job);
        }
    }

    private async Task ProcessJobAsync(PrintJob job)
    {
        try
        {
            _logger.LogInformation($"开始处理任务: {job.JobId}");
            
            job.Status = PrintJobStatus.Printing;
            job.Message = "正在打印...";
            _jobStore.UpdateJob(job);

            var success = await Task.Run(() => 
                _printerService.PrintFile(job.FileId, job.PrinterName, job.Copies, job.Options));

            if (success)
            {
                job.Status = PrintJobStatus.Completed;
                job.Message = "打印完成";
                job.CompletedAt = DateTime.Now;
                _logger.LogInformation($"任务完成: {job.JobId}");
            }
            else
            {
                await HandleFailureAsync(job);
            }

            _jobStore.UpdateJob(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"处理任务失败: {job.JobId}");
            await HandleFailureAsync(job);
            _jobStore.UpdateJob(job);
        }
    }

    private async Task HandleFailureAsync(PrintJob job)
    {
        job.RetryCount++;
        
        if (job.RetryCount < _maxRetries)
        {
            _logger.LogWarning($"任务失败，准备重试 ({job.RetryCount}/{_maxRetries}): {job.JobId}");
            job.Status = PrintJobStatus.Pending;
            job.Message = $"打印失败，正在重试 ({job.RetryCount}/{_maxRetries})";
            
            await Task.Delay(2000); // 延迟 2 秒后重试
            EnqueueJob(job);
        }
        else
        {
            _logger.LogError($"任务失败，已达最大重试次数: {job.JobId}");
            job.Status = PrintJobStatus.Failed;
            job.Message = $"打印失败，已重试 {_maxRetries} 次";
            job.CompletedAt = DateTime.Now;
        }
    }
}
