using PrintServer.Models;

namespace PrintServer.Services;

public interface IJobStore
{
    void AddJob(PrintJob job);
    PrintJob? GetJob(string jobId);
    List<PrintJob> GetAllJobs();
    void UpdateJob(PrintJob job);
    Task SaveToFileAsync();
    Task LoadFromFileAsync();
}
