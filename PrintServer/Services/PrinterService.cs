using System.Drawing;
using System.Drawing.Printing;
using System.Diagnostics;
using PrintServer.Models;
using PaperSize = System.Drawing.Printing.PaperSize;

namespace PrintServer.Services;

public class PrinterService : IPrinterService
{
    private readonly ILogger<PrinterService> _logger;

    public PrinterService(ILogger<PrinterService> logger)
    {
        _logger = logger;
    }

    public List<string> GetPrinters()
    {
        try
        {
            var printers = new List<string>();
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                printers.Add(printer);
            }
            return printers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取打印机列表失败");
            return new List<string>();
        }
    }

    public List<PrinterInfo> GetPrintersDetailed()
    {
        var printers = new List<PrinterInfo>();
        
        try
        {
            foreach (string printerName in PrinterSettings.InstalledPrinters)
            {
                try
                {
                    var settings = new PrinterSettings { PrinterName = printerName };
                    
                    var info = new PrinterInfo
                    {
                        Name = printerName,
                        IsDefault = settings.IsDefaultPrinter,
                        IsOnline = settings.IsValid,
                        SupportsColor = settings.SupportsColor,
                        SupportsDuplex = settings.Duplex != Duplex.Simplex || settings.CanDuplex,
                        PrinterType = "Local", // 可以通过 WMI 获取更详细信息
                        PortName = "Unknown",
                        DriverName = "Unknown"
                    };

                    // 获取支持的纸张大小
                    foreach (PaperSize paperSize in settings.PaperSizes)
                    {
                        info.SupportedPaperSizes.Add(paperSize.PaperName);
                    }

                    // 获取打印机状态
                    info.Status = settings.IsValid ? "就绪" : "离线";

                    printers.Add(info);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"获取打印机 {printerName} 详细信息失败");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取打印机详细列表失败");
        }

        return printers;
    }

    public bool PrintFile(string filePath, string printerName, int copies, Models.PrintOptions? options = null)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogError($"文件不存在: {filePath}");
                return false;
            }

            var extension = Path.GetExtension(filePath).ToLower();
            
            return extension switch
            {
                ".pdf" => PrintPdf(filePath, printerName, copies, options),
                ".png" or ".jpg" or ".jpeg" or ".bmp" => PrintImage(filePath, printerName, copies, options),
                ".docx" or ".doc" or ".xlsx" or ".xls" => PrintOfficeFile(filePath, printerName, copies, options),
                ".txt" => PrintTextFile(filePath, printerName, copies, options),
                _ => throw new NotSupportedException($"不支持的文件格式: {extension}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"打印文件失败: {filePath}");
            return false;
        }
    }

    public bool CancelPrintJob(string printerName, int jobId)
    {
        // 取消打印任务需要使用 Windows API
        // 这里提供基本实现框架
        _logger.LogInformation($"尝试取消打印任务: {printerName}, JobId: {jobId}");
        return true;
    }

    private bool PrintPdf(string filePath, string printerName, int copies, Models.PrintOptions? options)
    {
        try
        {
            var collate = options?.Collate ?? true;
            
            // PDF 使用系统默认阅读器打印
            // 注意：Process.Start 的 "print" 动词无法直接控制 Collate
            // 通过循环打印实现逐份打印效果
            
            if (collate)
            {
                // 逐份打印：每次打印完整文档
                _logger.LogInformation($"PDF 逐份打印: {filePath}, 份数: {copies}");
                for (int i = 0; i < copies; i++)
                {
                    _logger.LogInformation($"打印第 {i + 1}/{copies} 份");
                    
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = filePath,
                        Verb = "print",
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        UseShellExecute = true
                    };

                    using var process = Process.Start(startInfo);
                    if (process == null)
                    {
                        _logger.LogError("启动打印进程失败");
                        return false;
                    }
                    
                    // 等待打印任务提交
                    Thread.Sleep(3000);
                    
                    // 多份之间延迟，避免打印队列混乱
                    if (i < copies - 1)
                    {
                        Thread.Sleep(2000);
                    }
                }
            }
            else
            {
                // 不逐份打印：一次性提交（依赖 PDF 阅读器）
                _logger.LogInformation($"PDF 不逐份打印: {filePath}, 份数: {copies}");
                _logger.LogWarning("PDF 不逐份打印模式依赖于 PDF 阅读器的默认行为");
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    Verb = "print",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    return false;
                }
                
                Thread.Sleep(3000);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打印 PDF 失败");
            return false;
        }
    }

    private bool PrintImage(string filePath, string printerName, int copies, Models.PrintOptions? options)
    {
        try
        {
            using var image = Image.FromFile(filePath);
            var printDocument = new PrintDocument
            {
                PrinterSettings = { 
                    PrinterName = printerName, 
                    Copies = (short)copies,
                    Collate = options?.Collate ?? true  // 逐份打印（默认开启）
                }
            };

            // 应用打印选项
            ApplyPrintOptions(printDocument, options);

            printDocument.PrintPage += (sender, e) =>
            {
                if (e.Graphics != null)
                {
                    var bounds = e.MarginBounds;
                    var imageRatio = (float)image.Width / image.Height;
                    var boundsRatio = (float)bounds.Width / bounds.Height;

                    int width, height;
                    
                    if (options?.FitToPage == true)
                    {
                        // 适应页面大小
                        if (imageRatio > boundsRatio)
                        {
                            width = bounds.Width;
                            height = (int)(bounds.Width / imageRatio);
                        }
                        else
                        {
                            height = bounds.Height;
                            width = (int)(bounds.Height * imageRatio);
                        }
                    }
                    else
                    {
                        // 按缩放比例
                        var scale = (options?.Scale ?? 100) / 100.0f;
                        width = (int)(image.Width * scale);
                        height = (int)(image.Height * scale);
                        
                        // 确保不超出边界
                        if (width > bounds.Width)
                        {
                            width = bounds.Width;
                            height = (int)(bounds.Width / imageRatio);
                        }
                        if (height > bounds.Height)
                        {
                            height = bounds.Height;
                            width = (int)(bounds.Height * imageRatio);
                        }
                    }

                    // 居中
                    int x = bounds.X + (bounds.Width - width) / 2;
                    int y = bounds.Y + (bounds.Height - height) / 2;

                    e.Graphics.DrawImage(image, x, y, width, height);
                }
            };

            printDocument.Print();
            _logger.LogInformation($"图片打印完成: {filePath}, 份数: {copies}, 逐份打印: {options?.Collate ?? true}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打印图片失败");
            return false;
        }
    }

    private bool PrintOfficeFile(string filePath, string printerName, int copies, Models.PrintOptions? options)
    {
        try
        {
            var collate = options?.Collate ?? true;
            
            // Office 文件使用 Windows 默认程序打印
            // 注意：Process.Start 的 "print" 动词无法直接控制 Collate
            // 通过循环打印实现逐份打印效果
            
            if (collate)
            {
                // 逐份打印：每次打印完整文档
                _logger.LogInformation($"Office 逐份打印: {filePath}, 份数: {copies}");
                for (int i = 0; i < copies; i++)
                {
                    _logger.LogInformation($"打印第 {i + 1}/{copies} 份");
                    
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = filePath,
                        Verb = "print",
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        UseShellExecute = true
                    };

                    using var process = Process.Start(startInfo);
                    if (process == null)
                    {
                        _logger.LogError("启动打印进程失败");
                        return false;
                    }
                    
                    // Office 文件需要更长时间
                    Thread.Sleep(5000);
                    
                    // 多份之间延迟，避免打印队列混乱
                    if (i < copies - 1)
                    {
                        Thread.Sleep(3000);
                    }
                }
            }
            else
            {
                // 不逐份打印：一次性提交（依赖 Office 的默认行为）
                _logger.LogInformation($"Office 不逐份打印: {filePath}, 份数: {copies}");
                _logger.LogWarning("Office 不逐份打印模式依赖于 Office 的默认行为");
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    Verb = "print",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    return false;
                }
                
                Thread.Sleep(5000);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打印 Office 文件失败");
            return false;
        }
    }

    private bool PrintTextFile(string filePath, string printerName, int copies, Models.PrintOptions? options)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            var printDocument = new PrintDocument
            {
                PrinterSettings = { 
                    PrinterName = printerName, 
                    Copies = (short)copies,
                    Collate = options?.Collate ?? true  // 逐份打印（默认开启）
                }
            };

            // 应用打印选项
            ApplyPrintOptions(printDocument, options);

            printDocument.PrintPage += (sender, e) =>
            {
                if (e.Graphics != null)
                {
                    var font = new Font("Consolas", 10);
                    var brush = Brushes.Black;
                    var bounds = e.MarginBounds;
                    
                    e.Graphics.DrawString(content, font, brush, bounds);
                }
            };

            printDocument.Print();
            _logger.LogInformation($"文本打印完成: {filePath}, 份数: {copies}, 逐份打印: {options?.Collate ?? true}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打印文本文件失败");
            return false;
        }
    }

    private void ApplyPrintOptions(PrintDocument printDocument, Models.PrintOptions? options)
    {
        if (options == null) return;

        try
        {
            // 设置双面打印
            printDocument.PrinterSettings.Duplex = options.Duplex switch
            {
                DuplexMode.Simplex => Duplex.Simplex,
                DuplexMode.DuplexLongEdge => Duplex.Vertical,
                DuplexMode.DuplexShortEdge => Duplex.Horizontal,
                _ => Duplex.Simplex
            };

            // 设置颜色模式
            printDocument.DefaultPageSettings.Color = options.Color == ColorMode.Color;

            // 设置页面方向
            printDocument.DefaultPageSettings.Landscape = options.Orientation == PageOrientation.Landscape;

            // 设置纸张大小
            var paperSize = GetPaperSize(printDocument.PrinterSettings, options.PaperSize);
            if (paperSize != null)
            {
                printDocument.DefaultPageSettings.PaperSize = paperSize;
            }

            _logger.LogInformation($"应用打印选项: 双面={options.Duplex}, 颜色={options.Color}, 方向={options.Orientation}, 纸张={options.PaperSize}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "应用打印选项失败，使用默认设置");
        }
    }

    private PaperSize? GetPaperSize(PrinterSettings settings, Models.PaperSize paperSize)
    {
        var paperName = paperSize switch
        {
            Models.PaperSize.A4 => "A4",
            Models.PaperSize.A3 => "A3",
            Models.PaperSize.A5 => "A5",
            Models.PaperSize.Letter => "Letter",
            Models.PaperSize.Legal => "Legal",
            Models.PaperSize.Tabloid => "Tabloid",
            _ => "A4"
        };

        foreach (PaperSize ps in settings.PaperSizes)
        {
            if (ps.PaperName.Contains(paperName, StringComparison.OrdinalIgnoreCase))
            {
                return ps;
            }
        }

        return null;
    }
}
