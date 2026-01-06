using System.Collections.Concurrent;
using System.Text.Json;
using PrintServer.Models;

namespace PrintServer.Services;

public class JobStore : IJobStore
{
    private readonly ConcurrentDictionary<string, PrintJob> _jobs = new();
    private readonly string _storageFile = "tasks.json";
    private readonly ILogger<JobStore> _logger;

    public JobStore(ILogger<JobStore> logger)
    {
        _logger = logger;
        _ = LoadFromFileAsync();
    }

    public void AddJob(PrintJob job)
    {
        _jobs[job.JobId] = job;
        _ = SaveToFileAsync();
    }

    public PrintJob? GetJob(string jobId)
    {
        _jobs.TryGetValue(jobId, out var job);
        return job;
    }

    public List<PrintJob> GetAllJobs()
    {
        return _jobs.Values.OrderByDescending(j => j.CreatedAt).ToList();
    }

    public void UpdateJob(PrintJob job)
    {
        _jobs[job.JobId] = job;
        _ = SaveToFileAsync();
    }

    public async Task SaveToFileAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_jobs.Values, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            await File.WriteAllTextAsync(_storageFile, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存任务文件失败");
        }
    }

    public async Task LoadFromFileAsync()
    {
        try
        {
            if (File.Exists(_storageFile))
            {
                var json = await File.ReadAllTextAsync(_storageFile);
                var jobs = JsonSerializer.Deserialize<List<PrintJob>>(json);
                if (jobs != null)
                {
                    foreach (var job in jobs)
                    {
                        _jobs[job.JobId] = job;
                    }
                    _logger.LogInformation($"已加载 {jobs.Count} 个任务");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载任务文件失败");
        }
    }
}
