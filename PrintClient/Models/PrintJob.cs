namespace PrintClient.Models;

public class PrintJob
{
    public string JobId { get; set; } = string.Empty;
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string PrinterName { get; set; } = string.Empty;
    public int Copies { get; set; } = 1;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class PrintRequest
{
    public string FileId { get; set; } = string.Empty;
    public string PrinterName { get; set; } = string.Empty;
    public int Copies { get; set; } = 1;
    public PrintOptions? Options { get; set; }
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
}

public class PrintResponse
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
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
