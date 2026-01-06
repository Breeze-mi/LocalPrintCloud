namespace PrintServer.Models;

public class PrintRequest
{
    public string FileId { get; set; } = string.Empty;
    public string PrinterName { get; set; } = string.Empty;
    public int Copies { get; set; } = 1;
    public PrintOptions? Options { get; set; }
}

public class UploadResponse
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}

public class PrintResponse
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class PrintJob
{
    public string JobId { get; set; } = Guid.NewGuid().ToString();
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string PrinterName { get; set; } = string.Empty;
    public int Copies { get; set; } = 1;
    public PrintOptions? Options { get; set; }
    public PrintJobStatus Status { get; set; } = PrintJobStatus.Pending;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? CompletedAt { get; set; }
    public int RetryCount { get; set; } = 0;
}

public enum PrintJobStatus
{
    Pending,
    Printing,
    Completed,
    Failed,
    Cancelled
}
