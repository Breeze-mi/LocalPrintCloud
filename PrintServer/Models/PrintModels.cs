namespace PrintServer.Models;

// 打印请求（支持多文件）
public class PrintRequest
{
    public List<string> FileIds { get; set; } = new();  // 支持多文件
    public string PrinterName { get; set; } = string.Empty;
    public int Copies { get; set; } = 1;
    public PrintOptions? Options { get; set; }
    public bool MergeFiles { get; set; } = false;  // 是否合并打印
    
    // 向后兼容：单文件模式
    public string? FileId 
    { 
        get => FileIds.Count > 0 ? FileIds[0] : null;
        set 
        {
            if (!string.IsNullOrEmpty(value))
            {
                FileIds = new List<string> { value };
            }
        }
    }
}

// 上传响应（增强）
public class UploadResponse
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }  // 文件大小（字节）
    public string FileType { get; set; } = string.Empty;  // 文件类型
    public FilePreview? Preview { get; set; }  // 文件预览信息
}

// 文件预览信息
public class FilePreview
{
    public int PageCount { get; set; }  // 页数
    public string Dimensions { get; set; } = string.Empty;  // 尺寸（如 "210x297mm"）
    public bool IsColor { get; set; }  // 是否彩色
    public string Format { get; set; } = string.Empty;  // 格式（PDF/Image/Document）
}

// 打印响应
public class PrintResponse
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public PrintStatistics? Statistics { get; set; }  // 打印统计
}

// 打印统计
public class PrintStatistics
{
    public int TotalPages { get; set; }  // 总页数
    public int ColorPages { get; set; }  // 彩色页数
    public int BlackWhitePages { get; set; }  // 黑白页数
    public decimal EstimatedCost { get; set; }  // 预估费用
    public string CostBreakdown { get; set; } = string.Empty;  // 费用明细
}

// 打印任务（增强）
public class PrintJob
{
    public string JobId { get; set; } = Guid.NewGuid().ToString();
    public List<string> FileIds { get; set; } = new();  // 支持多文件
    public string FileName { get; set; } = string.Empty;
    public string PrinterName { get; set; } = string.Empty;
    public int Copies { get; set; } = 1;
    public PrintOptions? Options { get; set; }
    public PrintJobStatus Status { get; set; } = PrintJobStatus.Pending;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? CompletedAt { get; set; }
    public int RetryCount { get; set; } = 0;
    public PrintStatistics? Statistics { get; set; }  // 打印统计
    public bool MergeFiles { get; set; } = false;  // 是否合并打印
}

public enum PrintJobStatus
{
    Pending,
    Printing,
    Completed,
    Failed,
    Cancelled
}
