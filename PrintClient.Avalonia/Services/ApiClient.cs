using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using PrintClient.Avalonia.Models;

namespace PrintClient.Avalonia.Services;

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private const string ApiKey = "dev-token-123";

    public ApiClient()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };
    }

    public void SetBaseUrl(string baseUrl)
    {
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<List<string>> GetPrintersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/printers");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<string>>() ?? new List<string>();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"无法连接到服务器。请确保服务端正在运行。详细信息: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"获取打印机列表失败: {ex.Message}", ex);
        }
    }

    public async Task<List<PrinterInfo>> GetPrintersDetailedAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/printers/detailed");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<PrinterInfo>>() ?? new List<PrinterInfo>();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"无法连接到服务器。请确保服务端正在运行。详细信息: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"获取打印机详细信息失败: {ex.Message}", ex);
        }
    }

    public async Task<UploadResponse> UploadFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("文件不存在", filePath);
        }

        // 读取文件内容
        var fileBytes = await File.ReadAllBytesAsync(filePath);
        var fileName = Path.GetFileName(filePath);

        // 创建 multipart content
        var boundary = "----WebKitFormBoundary" + Guid.NewGuid().ToString("N");
        var content = new MultipartFormDataContent(boundary);
        
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = "file",
            FileName = fileName
        };
        
        content.Add(fileContent);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/upload")
        {
            Content = content
        };
        request.Headers.Add("X-Api-Key", ApiKey);

        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"上传失败 ({response.StatusCode}): {errorContent}");
        }

        return await response.Content.ReadFromJsonAsync<UploadResponse>() 
            ?? throw new Exception("上传失败：无法解析响应");
    }

    public async Task<PrintResponse> PrintAsync(PrintRequest request)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/print")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("X-Api-Key", ApiKey);

        var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PrintResponse>() 
            ?? throw new Exception("打印请求失败");
    }

    public async Task<PrintJob> GetJobStatusAsync(string jobId)
    {
        var response = await _httpClient.GetAsync($"/api/status/{jobId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PrintJob>() 
            ?? throw new Exception("获取任务状态失败");
    }

    public async Task<List<PrintJob>> GetAllJobsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/jobs");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<PrintJob>>() ?? new List<PrintJob>();
        }
        catch (HttpRequestException)
        {
            // 服务器未连接时返回空列表，不抛出异常
            return new List<PrintJob>();
        }
        catch (Exception)
        {
            return new List<PrintJob>();
        }
    }
}
