using System.Collections.Generic;
using System.Threading.Tasks;
using PrintClient.Avalonia.Models;

namespace PrintClient.Avalonia.Services;

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
