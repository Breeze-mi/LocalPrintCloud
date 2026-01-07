using System;
using System.Collections.Generic;

namespace PrintClient.Avalonia.Models;

public class PrintJob
{
    public string JobId { get; set; } = string.Empty;
    public List<string> FileIds { get; set; } = new();
    public string FileName { get; set; } = string.Empty;
    public string PrinterName { get; set; } = string.Empty;
    public int Copies { get; set; } = 1;
    public PrintJobStatus Status { get; set; } = PrintJobStatus.Pending;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public PrintStatistics? Statistics { get; set; }
    
    // 显示属性
    public string StatusDisplay => Status switch
    {
        PrintJobStatus.Pending => "⏳ 等待中",
        PrintJobStatus.Printing => "🖨️ 打印中",
        PrintJobStatus.Completed => "✅ 已完成",
        PrintJobStatus.Failed => "❌ 失败",
        PrintJobStatus.Cancelled => "🚫 已取消",
        _ => "❓ 未知"
    };
    
    public string StatusColor => Status switch
    {
        PrintJobStatus.Pending => "#FF9800",
        PrintJobStatus.Printing => "#2196F3",
        PrintJobStatus.Completed => "#4CAF50",
        PrintJobStatus.Failed => "#F44336",
        PrintJobStatus.Cancelled => "#9E9E9E",
        _ => "#000000"
    };
    
    public string CreatedAtDisplay => CreatedAt.ToString("HH:mm:ss");
    
    public string CostDisplay => Statistics != null 
        ? $"¥{Statistics.EstimatedCost:F2}" 
        : "-";
    
    public string PagesDisplay => Statistics != null 
        ? $"{Statistics.TotalPages} 页" 
        : "-";
}

public enum PrintJobStatus
{
    Pending = 0,
    Printing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}

public class PrintRequest
{
    public List<string> FileIds { get; set; } = new();  // 支持多文件
    public string PrinterName { get; set; } = string.Empty;
    public int Copies { get; set; } = 1;
    public PrintOptions? Options { get; set; }
    public bool MergeFiles { get; set; } = false;  // 是否合并打印
    
    // 向后兼容：单文件模式
    public string FileId 
    { 
        get => FileIds.Count > 0 ? FileIds[0] : string.Empty;
        set 
        {
            if (!string.IsNullOrEmpty(value))
            {
                FileIds = new List<string> { value };
            }
        }
    }
}

public class PrintOptions
{
    public int Duplex { get; set; } = 0;           // 0=单面, 1=双面长边, 2=双面短边
    public int Color { get; set; } = 1;            // 0=黑白, 1=彩色
    public int PaperSize { get; set; } = 0;        // 0=A4, 1=A3, 2=A5, 3=Letter, 4=Legal, 5=Tabloid
    public int Orientation { get; set; } = 0;      // 0=纵向, 1=横向
    public int Quality { get; set; } = 1;          // 0=草稿, 1=标准, 2=高质量
    public string? PageRange { get; set; }
    public int Scale { get; set; } = 100;
    public int PagesPerSheet { get; set; } = 1;
    public bool AutoRotateAndCenter { get; set; } = true;
    public bool FitToPage { get; set; } = false;
}

public class UploadResponse
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileType { get; set; } = string.Empty;
    public FilePreview? Preview { get; set; }
}

public class FilePreview
{
    public int PageCount { get; set; }
    public string Dimensions { get; set; } = string.Empty;
    public bool IsColor { get; set; }
    public string Format { get; set; } = string.Empty;
}

public class PrintResponse
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public PrintStatistics? Statistics { get; set; }
}

public class PrintStatistics
{
    public int TotalPages { get; set; }
    public int ColorPages { get; set; }
    public int BlackWhitePages { get; set; }
    public decimal EstimatedCost { get; set; }
    public string CostBreakdown { get; set; } = string.Empty;
}

public class PrinterInfo
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsOnline { get; set; }
    public bool SupportsColor { get; set; }
    public bool SupportsDuplex { get; set; }
    public List<string> SupportedPaperSizes { get; set; } = new();
    public string PrinterType { get; set; } = string.Empty;
    public string PortName { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
}
