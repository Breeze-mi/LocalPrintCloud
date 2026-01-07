using PrintServer.Models;

namespace PrintServer.Services;

public interface IPrintStatisticsService
{
    PrintStatistics CalculateStatistics(List<string> fileIds, PrintOptions? options, int copies);
    Task<List<PrintHistoryRecord>> GetPrintHistoryAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<PrintSummary> GetPrintSummaryAsync(DateTime? startDate = null, DateTime? endDate = null);
}

public class PrintStatisticsService : IPrintStatisticsService
{
    private readonly ILogger<PrintStatisticsService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _uploadFolder;
    
    // 价格配置（可从配置文件读取）
    private readonly decimal _pricePerBlackWhitePage;
    private readonly decimal _pricePerColorPage;

    public PrintStatisticsService(
        ILogger<PrintStatisticsService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _uploadFolder = configuration["UploadFolder"] ?? "uploads";
        
        // 从配置读取价格，默认值：黑白 0.1 元/页，彩色 0.5 元/页
        _pricePerBlackWhitePage = decimal.Parse(configuration["PricePerBlackWhitePage"] ?? "0.1");
        _pricePerColorPage = decimal.Parse(configuration["PricePerColorPage"] ?? "0.5");
    }

    public PrintStatistics CalculateStatistics(List<string> fileIds, PrintOptions? options, int copies)
    {
        var statistics = new PrintStatistics();
        
        try
        {
            int totalPages = 0;
            
            foreach (var fileId in fileIds)
            {
                var filePath = Path.Combine(_uploadFolder, Path.GetFileName(fileId));
                if (File.Exists(filePath))
                {
                    var pageCount = GetPageCount(filePath);
                    totalPages += pageCount;
                }
            }

            // 应用页码范围
            if (options?.PageRange != null && !string.IsNullOrEmpty(options.PageRange))
            {
                var selectedPages = ParsePageRange(options.PageRange, totalPages);
                totalPages = selectedPages.Count;
            }

            // 应用份数
            totalPages *= copies;

            // 计算彩色和黑白页数
            var isColor = options?.Color == ColorMode.Color;
            statistics.TotalPages = totalPages;
            statistics.ColorPages = isColor ? totalPages : 0;
            statistics.BlackWhitePages = isColor ? 0 : totalPages;

            // 计算费用
            statistics.EstimatedCost = 
                (statistics.BlackWhitePages * _pricePerBlackWhitePage) +
                (statistics.ColorPages * _pricePerColorPage);

            // 费用明细
            var breakdown = new List<string>();
            if (statistics.BlackWhitePages > 0)
            {
                breakdown.Add($"黑白: {statistics.BlackWhitePages} 页 × ¥{_pricePerBlackWhitePage} = ¥{statistics.BlackWhitePages * _pricePerBlackWhitePage:F2}");
            }
            if (statistics.ColorPages > 0)
            {
                breakdown.Add($"彩色: {statistics.ColorPages} 页 × ¥{_pricePerColorPage} = ¥{statistics.ColorPages * _pricePerColorPage:F2}");
            }
            breakdown.Add($"总计: ¥{statistics.EstimatedCost:F2}");
            
            statistics.CostBreakdown = string.Join("\n", breakdown);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "计算打印统计失败");
        }

        return statistics;
    }

    private int GetPageCount(string filePath)
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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"获取页数失败: {filePath}");
            return 1;
        }
    }

    private int GetPdfPageCount(string filePath)
    {
        try
        {
            // 简单的 PDF 页数检测（读取文件内容查找 /Count 标记）
            var content = File.ReadAllText(filePath);
            var match = System.Text.RegularExpressions.Regex.Match(content, @"/Count\s+(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var count))
            {
                return count;
            }
        }
        catch { }
        
        return 1;  // 默认 1 页
    }

    private int EstimateTextPageCount(string filePath)
    {
        try
        {
            var lines = File.ReadAllLines(filePath).Length;
            // 假设每页 50 行
            return Math.Max(1, (lines + 49) / 50);
        }
        catch
        {
            return 1;
        }
    }

    private int EstimateOfficePageCount(string filePath)
    {
        // Office 文档页数估算（基于文件大小）
        try
        {
            var fileInfo = new FileInfo(filePath);
            var sizeInKB = fileInfo.Length / 1024;
            
            // 粗略估算：每 50KB 约 1 页
            return Math.Max(1, (int)(sizeInKB / 50));
        }
        catch
        {
            return 1;
        }
    }

    private List<int> ParsePageRange(string pageRange, int totalPages)
    {
        var pages = new HashSet<int>();
        
        try
        {
            var parts = pageRange.Split(',', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                
                if (trimmed.Contains('-'))
                {
                    // 范围：1-5
                    var range = trimmed.Split('-');
                    if (range.Length == 2 &&
                        int.TryParse(range[0].Trim(), out var start) &&
                        int.TryParse(range[1].Trim(), out var end))
                    {
                        for (int i = start; i <= end && i <= totalPages; i++)
                        {
                            if (i > 0) pages.Add(i);
                        }
                    }
                }
                else
                {
                    // 单页：8
                    if (int.TryParse(trimmed, out var page) && page > 0 && page <= totalPages)
                    {
                        pages.Add(page);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"解析页码范围失败: {pageRange}");
        }

        return pages.OrderBy(p => p).ToList();
    }

    public Task<List<PrintHistoryRecord>> GetPrintHistoryAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        // TODO: 从数据库或文件读取历史记录
        return Task.FromResult(new List<PrintHistoryRecord>());
    }

    public Task<PrintSummary> GetPrintSummaryAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        // TODO: 统计汇总
        return Task.FromResult(new PrintSummary());
    }
}

// 打印历史记录
public class PrintHistoryRecord
{
    public string JobId { get; set; } = string.Empty;
    public DateTime PrintTime { get; set; }
    public int Pages { get; set; }
    public decimal Cost { get; set; }
    public string Status { get; set; } = string.Empty;
}

// 打印汇总
public class PrintSummary
{
    public int TotalJobs { get; set; }
    public int TotalPages { get; set; }
    public decimal TotalCost { get; set; }
    public int SuccessfulJobs { get; set; }
    public int FailedJobs { get; set; }
}
