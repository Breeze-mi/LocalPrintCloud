using Microsoft.AspNetCore.Mvc;
using PrintServer.Services;

namespace PrintServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatisticsController : ControllerBase
{
    private readonly IPrintStatisticsService _statisticsService;
    private readonly IJobStore _jobStore;
    private readonly ILogger<StatisticsController> _logger;

    public StatisticsController(
        IPrintStatisticsService statisticsService,
        IJobStore jobStore,
        ILogger<StatisticsController> logger)
    {
        _statisticsService = statisticsService;
        _jobStore = jobStore;
        _logger = logger;
    }

    // 获取打印历史
    [HttpGet("history")]
    public async Task<ActionResult<List<PrintHistoryRecord>>> GetHistory(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var history = await _statisticsService.GetPrintHistoryAsync(startDate, endDate);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取打印历史失败");
            return BadRequest(new { error = ex.Message });
        }
    }

    // 获取打印汇总
    [HttpGet("summary")]
    public async Task<ActionResult<PrintSummary>> GetSummary(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            // 从任务存储计算汇总
            var allJobs = _jobStore.GetAllJobs();
            
            var filteredJobs = allJobs.Where(j =>
            {
                if (startDate.HasValue && j.CreatedAt < startDate.Value)
                    return false;
                if (endDate.HasValue && j.CreatedAt > endDate.Value)
                    return false;
                return true;
            }).ToList();

            var summary = new PrintSummary
            {
                TotalJobs = filteredJobs.Count,
                TotalPages = filteredJobs.Sum(j => j.Statistics?.TotalPages ?? 0),
                TotalCost = filteredJobs.Sum(j => j.Statistics?.EstimatedCost ?? 0),
                SuccessfulJobs = filteredJobs.Count(j => j.Status == Models.PrintJobStatus.Completed),
                FailedJobs = filteredJobs.Count(j => j.Status == Models.PrintJobStatus.Failed)
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取打印汇总失败");
            return BadRequest(new { error = ex.Message });
        }
    }

    // 获取今日统计
    [HttpGet("today")]
    public ActionResult<object> GetTodayStatistics()
    {
        try
        {
            var today = DateTime.Today;
            var allJobs = _jobStore.GetAllJobs();
            
            var todayJobs = allJobs.Where(j => j.CreatedAt.Date == today).ToList();

            var stats = new
            {
                Date = today.ToString("yyyy-MM-dd"),
                TotalJobs = todayJobs.Count,
                CompletedJobs = todayJobs.Count(j => j.Status == Models.PrintJobStatus.Completed),
                FailedJobs = todayJobs.Count(j => j.Status == Models.PrintJobStatus.Failed),
                PendingJobs = todayJobs.Count(j => j.Status == Models.PrintJobStatus.Pending),
                TotalPages = todayJobs.Sum(j => j.Statistics?.TotalPages ?? 0),
                TotalCost = todayJobs.Sum(j => j.Statistics?.EstimatedCost ?? 0),
                ColorPages = todayJobs.Sum(j => j.Statistics?.ColorPages ?? 0),
                BlackWhitePages = todayJobs.Sum(j => j.Statistics?.BlackWhitePages ?? 0)
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取今日统计失败");
            return BadRequest(new { error = ex.Message });
        }
    }

    // 获取本周统计
    [HttpGet("week")]
    public ActionResult<object> GetWeekStatistics()
    {
        try
        {
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            var allJobs = _jobStore.GetAllJobs();
            
            var weekJobs = allJobs.Where(j => j.CreatedAt >= startOfWeek).ToList();

            var dailyStats = weekJobs
                .GroupBy(j => j.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    Jobs = g.Count(),
                    Pages = g.Sum(j => j.Statistics?.TotalPages ?? 0),
                    Cost = g.Sum(j => j.Statistics?.EstimatedCost ?? 0)
                })
                .OrderBy(s => s.Date)
                .ToList();

            return Ok(new
            {
                StartDate = startOfWeek.ToString("yyyy-MM-dd"),
                EndDate = today.ToString("yyyy-MM-dd"),
                TotalJobs = weekJobs.Count,
                TotalPages = weekJobs.Sum(j => j.Statistics?.TotalPages ?? 0),
                TotalCost = weekJobs.Sum(j => j.Statistics?.EstimatedCost ?? 0),
                DailyStats = dailyStats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取本周统计失败");
            return BadRequest(new { error = ex.Message });
        }
    }
}
