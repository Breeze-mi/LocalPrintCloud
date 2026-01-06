using PrintClient.Models;

namespace PrintClient.Services;

public interface IApiClient
{
    void SetBaseUrl(string baseUrl);
    Task<List<string>> GetPrintersAsync();
    Task<List<PrinterInfo>> GetPrintersDetailedAsync();
    Task<UploadResponse> UploadFileAsync(string filePath);
    Task<PrintResponse> PrintAsync(PrintRequest request);
    Task<PrintJob> GetJobStatusAsync(string jobId);
    Task<List<PrintJob>> GetAllJobsAsync();
}
