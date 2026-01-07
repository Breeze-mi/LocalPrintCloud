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
    private readonly bool _autoDeleteFiles;
    private readonly string _uploadFolder;
    private readonly int _deleteDelayMinutes;
    private readonly Timer _cleanupTimer;

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
        _autoDeleteFiles = bool.Parse(_configuration["AutoDeleteFiles"] ?? "true");
        _uploadFolder = _configuration["UploadFolder"] ?? "uploads";
        _deleteDelayMinutes = int.Parse(_configuration["DeleteDelayMinutes"] ?? "5");
        
        // 启动定时清理任务（每分钟检查一次）
        _cleanupTimer = new Timer(CleanupOldFiles, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
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

            bool success;
            
            // 支持多文件打印
            if (job.FileIds != null && job.FileIds.Count > 0)
            {
                if (job.MergeFiles && job.FileIds.Count > 1)
                {
                    // 合并打印（按顺序打印所有文件）
                    success = await Task.Run(() => PrintMultipleFiles(job));
                }
                else
                {
                    // 分别打印每个文件
                    success = await Task.Run(() => PrintMultipleFiles(job));
                }
            }
            else
            {
                // 向后兼容：单文件打印
                success = false;
            }

            if (success)
            {
                job.Status = PrintJobStatus.Completed;
                job.Message = "打印完成";
                job.CompletedAt = DateTime.Now;
                _logger.LogInformation($"任务完成: {job.JobId}");
                
                // 不立即删除，而是标记删除时间
                // 文件将在 DeleteDelayMinutes 分钟后被定时任务删除
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

    private bool PrintMultipleFiles(PrintJob job)
    {
        try
        {
            foreach (var fileId in job.FileIds)
            {
                _logger.LogInformation($"打印文件: {fileId}");
                
                var success = _printerService.PrintFile(fileId, job.PrinterName, job.Copies, job.Options);
                
                if (!success)
                {
                    _logger.LogError($"打印文件失败: {fileId}");
                    return false;
                }
                
                // 文件之间延迟 1 秒
                Thread.Sleep(1000);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打印多文件失败");
            return false;
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
            
            // 失败的任务也不立即删除，等待定时清理
        }
    }

    private void CleanupOldFiles(object? state)
    {
        if (!_autoDeleteFiles)
        {
            return;
        }

        try
        {
            var allJobs = _jobStore.GetAllJobs();
            var now = DateTime.Now;

            foreach (var job in allJobs)
            {
                // 只处理已完成或失败的任务
                if (job.Status != PrintJobStatus.Completed && job.Status != PrintJobStatus.Failed)
                {
                    continue;
                }

                // 检查是否超过延迟时间
                if (job.CompletedAt.HasValue)
                {
                    var timeSinceCompletion = now - job.CompletedAt.Value;
                    
                    if (timeSinceCompletion.TotalMinutes >= _deleteDelayMinutes)
                    {
                        // 支持多文件删除
                        if (job.FileIds != null && job.FileIds.Count > 0)
                        {
                            foreach (var fileId in job.FileIds)
                            {
                                DeleteFile(fileId);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "定时清理文件失败");
        }
    }

    private void DeleteFile(string fileId)
    {
        try
        {
            var filePath = Path.Combine(_uploadFolder, Path.GetFileName(fileId));
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation($"已删除文件: {filePath} (延迟 {_deleteDelayMinutes} 分钟后删除)");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"删除文件失败: {fileId}");
        }
    }
}
