using PrintServer.Models;
using System.Drawing;

namespace PrintServer.Services;

public interface IFilePreviewService
{
    Task<FilePreview> GeneratePreviewAsync(string filePath);
}

public class FilePreviewService : IFilePreviewService
{
    private readonly ILogger<FilePreviewService> _logger;

    public FilePreviewService(ILogger<FilePreviewService> logger)
    {
        _logger = logger;
    }

    public async Task<FilePreview> GeneratePreviewAsync(string filePath)
    {
        var preview = new FilePreview();
        
        try
        {
            var extension = Path.GetExtension(filePath).ToLower();
            
            preview.Format = extension switch
            {
                ".pdf" => "PDF",
                ".png" or ".jpg" or ".jpeg" or ".bmp" => "Image",
                ".docx" or ".doc" => "Word Document",
                ".xlsx" or ".xls" => "Excel Spreadsheet",
                ".txt" => "Text",
                _ => "Unknown"
            };

            preview.PageCount = await GetPageCountAsync(filePath);
            preview.Dimensions = await GetDimensionsAsync(filePath);
            preview.IsColor = await DetectColorAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"生成文件预览失败: {filePath}");
        }

        return preview;
    }

    private Task<int> GetPageCountAsync(string filePath)
    {
        return Task.Run(() =>
        {
            try
            {
                var extension = Path.GetExtension(filePath).ToLower();
                
                return extension switch
                {
                    ".pdf" => GetPdfPageCount(filePath),
                    ".png" or ".jpg" or ".jpeg" or ".bmp" => 1,
                    ".txt" => EstimateTextPageCount(filePath),
                    ".docx" or ".doc" or ".xlsx" or ".xls" => EstimateOfficePageCount(filePath),
                    _ => 1
                };
            }
            catch
            {
                return 1;
            }
        });
    }

    private Task<string> GetDimensionsAsync(string filePath)
    {
        return Task.Run(() =>
        {
            try
            {
                var extension = Path.GetExtension(filePath).ToLower();
                
                if (extension is ".png" or ".jpg" or ".jpeg" or ".bmp")
                {
                    using var image = Image.FromFile(filePath);
                    return $"{image.Width}x{image.Height} px";
                }
                
                // 默认 A4 尺寸
                return "210x297 mm (A4)";
            }
            catch
            {
                return "Unknown";
            }
        });
    }

    private Task<bool> DetectColorAsync(string filePath)
    {
        return Task.Run(() =>
        {
            try
            {
                var extension = Path.GetExtension(filePath).ToLower();
                
                if (extension is ".png" or ".jpg" or ".jpeg" or ".bmp")
                {
                    // 简单检测：检查图片是否有彩色像素
                    using var image = new Bitmap(filePath);
                    
                    // 采样检测（检查前 100 个像素）
                    int sampleCount = Math.Min(100, image.Width * image.Height);
                    for (int i = 0; i < sampleCount; i++)
                    {
                        int x = i % image.Width;
                        int y = i / image.Width;
                        var pixel = image.GetPixel(x, y);
                        
                        // 如果 RGB 值不相等，说明是彩色
                        if (pixel.R != pixel.G || pixel.G != pixel.B)
                        {
                            return true;
                        }
                    }
                    return false;
                }
                
                // 其他文件类型默认为彩色
                return true;
            }
            catch
            {
                return true;
            }
        });
    }

    private int GetPdfPageCount(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            var match = System.Text.RegularExpressions.Regex.Match(content, @"/Count\s+(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var count))
            {
                return count;
            }
        }
        catch { }
        
        return 1;
    }

    private int EstimateTextPageCount(string filePath)
    {
        try
        {
            var lines = File.ReadAllLines(filePath).Length;
            return Math.Max(1, (lines + 49) / 50);
        }
        catch
        {
            return 1;
        }
    }

    private int EstimateOfficePageCount(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            var sizeInKB = fileInfo.Length / 1024;
            return Math.Max(1, (int)(sizeInKB / 50));
        }
        catch
        {
            return 1;
        }
    }
}
